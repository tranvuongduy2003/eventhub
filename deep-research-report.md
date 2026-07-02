# Kế hoạch triển khai Harness cho source code làm việc với Codex

## Cách hiểu đúng sơ đồ hiện tại

Theo cách tôi đọc lại sơ đồ bạn gửi, **Harness** không nên được hiểu là một “skill” hay một cụm lệnh CLI, mà là **lớp hợp đồng và điều phối bao quanh model**: nó quyết định model nào chạy, tool nào được gọi, state nào được giữ, hook nào được kích hoạt, chính sách bảo vệ nào được áp dụng, và telemetry nào được ghi lại. Cách hiểu này rất khớp với định nghĩa “harness” trong tài liệu OpenAI Cookbook: *harness là toàn bộ contract quanh model, gồm instructions, tools, routing, output requirements và validation checks*. Đồng thời, OpenAI hiện mô tả các primitive cốt lõi của agent system là **models, tools, state/memory, orchestration**; còn khi ứng dụng tự sở hữu orchestration, execution, approvals và state thì nên dùng **Agents SDK**, không chỉ một lời gọi model đơn lẻ. citeturn15view0turn5view9turn5view3

![Sơ đồ Harness đã gửi](sandbox:/mnt/data/a44b2700-9b54-4336-a296-dc8fa8bb1d1d.png)

Nếu bóc tách sơ đồ theo hướng đó, phần lõi của Harness trong ảnh gồm các nhánh hợp lý sau: **Scheduler**, **Memory**, **Provider/Model**, **Tools**, **Skills**, **Hooks**, **Security**, và **Monitoring**. Cấu trúc này cũng khá sát với những gì OpenAI coi là một agent stack hiện đại: orchestration, tool-calling, state, handoffs, guardrails, traces và khả năng review lại toàn bộ workflow. citeturn5view9turn5view4turn5view5

Điểm rất quan trọng trong yêu cầu của bạn là nhánh có các ghi chú đỏ như **“codex tạo skill sử dụng kubectl CLI commands”**, **“Kubernetes Cluster kubectl”**, **“goclaw traces read abc”** chỉ là **ví dụ/chuẩn hóa cách làm skill hoặc CLI**, **không phải thành phần của Harness**. Tôi đồng ý với cách tách này. Về mặt triển khai, những dòng đó nên được xem là “**skill examples**” hoặc “**agent-friendly CLI standards**”, không được nhét vào kiến trúc lõi của Harness. Điều này còn phù hợp với tài liệu Codex: skills là workflow tái sử dụng; còn khi agent phải dùng đi dùng lại cùng một API, log source, database hay script, OpenAI khuyến nghị tạo **một CLI có interface ổn định** và **một companion skill** để dạy Codex khi nào dùng CLI đó. citeturn5view6turn5view0turn12view0

Tóm lại, cách hiểu đúng nhất của sơ đồ là:

- **Harness** là tầng điều phối và chính sách.
- **Tools** là bề mặt thực thi.
- **Skills** là tri thức workflow tái sử dụng.
- **AGENTS.md** và repo rules là guidance bền vững cho Codex.
- Các ví dụ kubectl/goclaw nằm ở nhánh kỹ thuật hóa skill/CLI, không nằm trong core Harness. citeturn11view2turn12view0turn10search7

## Những điểm cần sửa trong sơ đồ

Sơ đồ hiện tại có ý tưởng tốt, nhưng đang trộn lẫn **kiến trúc**, **quy ước tác nghiệp**, và **ví dụ minh họa**. Nếu giữ nguyên, khi đem đi triển khai vào source code thật, team rất dễ biến Harness thành một “đống prompt + scripts”, thay vì một lớp điều phối có ranh giới rõ ràng.

Điểm cần sửa đầu tiên là **tách Skills khỏi CLI examples**. Trong Codex, skill là một thư mục có `SKILL.md` cùng script/references tùy chọn, và Codex chỉ nạp metadata trước, rồi mới đọc toàn bộ nội dung khi quyết định dùng skill. Nghĩa là skill là **workflow package**, chứ không phải chính CLI. CLI chỉ là một execution surface mà skill có thể gọi tới. Vì vậy, trên sơ đồ mới nên có một box riêng như **“Skill Examples / CLI Design Standards”** đặt ngoài core Harness, nối nét đứt tới nhánh Skills và Tools. citeturn5view0turn12view0turn5view6

Điểm cần sửa thứ hai là **gộp lại đúng ranh giới giữa Security, Guardrails, Filters, Blocks và Hooks**. Theo thực tiễn agent stack hiện nay, guardrails nên được hiểu là **kiểm tra input/output hoặc chặn hành vi không mong muốn**, còn hooks là **điểm can thiệp vào lifecycle** như pre-run, pre-tool, post-tool, pre-output. OpenAI Agents SDK cũng tách khá rõ guardrails như một primitive riêng, đồng thời tracing có thể ghi nhận guardrails nào đã bị kích hoạt trong một run. Vì vậy, đề xuất tốt hơn là:  
**Hooks = lifecycle interception**;  
**Guardrails = policy checks**;  
**Filters/Blocks = concrete enforcement actions** của guardrails/security policy. citeturn5view9turn5view4turn2search11

Điểm cần sửa thứ ba là **làm rõ Memory**. Sơ đồ đang có “Working Memory”, “Internal Memory”, “External Memory”, “MEMORY.md”, “Notion”, “Obsidian”, nhưng chưa phân biệt giữa:  
**conversation state ngắn hạn**,  
**artifact/state vận hành**,  
và **knowledge memory dài hạn**. Với stack OpenAI hiện tại, bạn có thể tách rất rõ ba lớp này: dùng **Conversations/Responses state** cho state ngắn hạn; dùng **artifacts/files** cho output vận hành; và dùng **Retrieval + vector stores** cho long-term knowledge memory. Với các tác vụ dài, có thể dùng **server-side compaction** để giữ ngữ cảnh mà không làm phình token vô hạn. citeturn16view1turn14view3turn14view1turn14view0

Điểm cần sửa thứ tư là **Monitoring đang quá hẹp**. Trong thực tế, monitoring cho agent không chỉ là logs/traces/analytics rời rạc; nó cần gắn trực tiếp với **model calls, tool calls, handoffs, guardrails, custom spans**. OpenAI Agents SDK đã có tracing built-in cho đúng những dữ liệu này, và còn có trace grading để biến traces thành dữ liệu đánh giá có cấu trúc. Vì vậy, trên sơ đồ mới, mũi tên sang Monitoring nên đi từ gần như mọi nhánh: provider/model, tools, skills, hooks và security. citeturn5view4turn5view5

Điểm cần sửa cuối cùng là **Provider/Model đang bị underspecified**. Nếu đây là source code làm việc với Codex trong giai đoạn 2026, tôi khuyên **đừng thiết kế mới trên Assistants API**. OpenAI hiện khuyến nghị dùng **Responses API cho mọi dự án mới**, và Assistants API đã bị deprecate với ngày shutdown là **26 tháng 8 năm 2026**. Điều này ảnh hưởng trực tiếp tới Harness, vì layer model/provider phải được thiết kế quanh **Responses**, **Conversations**, **tool calls**, **compaction**, và nếu cần **Agents SDK**. citeturn16view1turn16view0

## Kiến trúc Harness nên chọn

Kiến trúc tôi khuyên triển khai không phải một lớp đơn lẻ, mà là **mô hình hai tầng**: một tầng dành cho **Codex làm việc tốt trong repo**, và một tầng dành cho **ứng dụng/hệ thống orchestration có thể review, trace và tự cải thiện**.

### Tầng repo dành cho Codex

Ở tầng này, mục tiêu không phải “xây agent platform”, mà là **làm cho repository trở thành một môi trường agent-friendly**. Codex đọc `AGENTS.md` **trước khi bắt đầu làm việc**, và có cơ chế precedence từ global tới project tới thư mục con. Song song, Codex dò skill trong `.agents/skills` dọc từ thư mục hiện tại lên tới repo root. Mỗi skill là một package có `SKILL.md`, script, references và assets tùy chọn. Đây là nền tảng đúng để mã nguồn “làm việc với Codex” theo cách bền vững, có thể commit vào repo, review bằng PR, và chia sẻ cho cả team. citeturn11view2turn12view0turn10search2turn15view1

Tầng repo nên chứa ít nhất bốn hợp phần:

1. **`AGENTS.md` ở repo root**: quy ước build/test/lint, style, chuẩn review, thư mục tài liệu, quy tắc phụ thuộc, điều kiện cần trước khi mở PR. Codex docs khuyến nghị giữ file này ngắn, chính xác, thực dụng và đặt rule ở đúng mức thư mục. citeturn11view2turn10search2

2. **Repo-local skills trong `.agents/skills/`**: ví dụ `repo-bootstrap`, `verify-changed-code`, `docs-sync`, `release-check`, `incident-readonly`, `pr-draft-summary`. Mô hình này cũng là pattern OpenAI nói họ dùng để tăng throughput trong chính các repo Agents SDK của họ. citeturn15view1turn12view0

3. **Scripts ổn định, agent-friendly**: thay vì bắt Codex ghép nhiều lệnh shell mong manh, hãy tạo các script có output gọn, predictable, và tách rõ read/write. Điều này phù hợp với khuyến nghị “create a CLI Codex can use”, nhất là cho các task lặp lại cần paged search, exact reads by ID, predictable JSON, downloaded files, local indexes hoặc safe draft-before-write commands. citeturn5view6turn14view4

4. **Permission policy cho local execution**: Codex mặc định có thể đọc/chỉnh file trong workspace và chạy routine local commands; nhưng sẽ hỏi approval khi đi ra ngoài workspace hoặc cần network. Tầng repo nên mặc định hóa boundary này bằng config và permission profiles thay vì phó mặc cho từng phiên ngẫu nhiên. citeturn8view0turn8view1turn8view2

### Tầng orchestration dành cho Harness

Nếu bạn muốn Harness vượt khỏi mức “repo customizations” để trở thành một runtime điều phối có thể tự kiểm soát, đề xuất tốt nhất là dùng **OpenAI Agents SDK** kết hợp **Codex CLI như một MCP server**. OpenAI có tài liệu chính thức cho pattern này: Codex CLI có thể được expose qua `codex mcp-server`, rồi Agents SDK dùng MCP để xây workflow determinisitic, reviewable, có hand-offs, guardrails và full traces. citeturn11view0turn5view3turn12view1turn12view2

Ở tầng này, Harness nên gồm sáu lớp nội bộ:

**Orchestrator**. Thành phần trung tâm nhận task, chọn agent/skill/tool, định tuyến handoff, quản lý retries, approval requests và stop conditions. Đây là phần “application-owned orchestration” mà Agents SDK sinh ra để giải quyết. citeturn5view3turn5view9

**Execution surface**. Bao gồm ba loại tool:
- built-in hosted tools khi phù hợp, để giảm custom orchestration;
- MCP tools cho third-party/dev tools;
- local CLI/runtime packages cho logic của chính repository. OpenAI hiện khuyên ưu tiên hosted tools khi khớp use case, còn custom function/CLI thì dùng cho system nội bộ và domain-specific workflows. citeturn14view4turn5view2turn12view3

**State and memory**. Dùng Conversations/Responses state cho ngữ cảnh ngắn hạn; dùng file/artifact store cho đầu ra trung gian; dùng vector stores/Retrieval cho tri thức dài hạn; bật compaction cho tác vụ dài. citeturn16view1turn14view3turn14view1turn14view0

**Policy layer**. Tách rules, guardrails, approval policy, permission profile, và hook points. Về mặt triển khai, đừng để policy “ẩn trong prompt”; hãy để nó vừa có dạng config, vừa có dạng code hook, vừa có dạng eval target. citeturn8view0turn8view2turn5view9turn5view5

**Observability layer**. Mỗi run cần có trace ID, spans cho model/tool/handoff/guardrail, structured logs cho CLI executions, và metric ở cấp task/skill/tool. Agents SDK đã có tracing built-in; còn trace grading cho phép biến traces thành dữ liệu đánh giá tái sử dụng được. citeturn5view4turn5view5

**Improvement loop**. Đây là phần quan trọng nhất nhưng thường bị bỏ qua. OpenAI đã mô tả một vòng lặp rất phù hợp với yêu cầu “tự đánh giá, tự sửa sai, tự cải thiện”: lấy **real traces**, thêm feedback của người và model, chuyển feedback thành **evals**, rồi tạo **Codex-ready handoff** cho vòng sửa tiếp theo. citeturn15view0turn11view3

## Kế hoạch triển khai theo giai đoạn

Vì bạn nói “triển khai Harness cho một source code làm việc với Codex”, tôi khuyến nghị tiến hành theo ba đợt tăng dần, thay vì lao ngay vào multi-agent orchestration đầy đủ. Cách này tối ưu rủi ro, vì repo sẽ có ích cho Codex ngay từ tuần đầu, trong khi kiến trúc vẫn mở để tiến đến automation sau này.

### Giai đoạn nền tảng repo

Trong đợt đầu, mục tiêu là biến repo thành một môi trường làm việc tốt cho Codex mà **chưa cần** ứng dụng orchestration riêng.

Việc đầu tiên là viết một `AGENTS.md` gọn nhưng cứng ở repo root. File này phải trả lời đúng các câu hỏi mà Codex cần mỗi ngày: lệnh setup, lệnh test/lint/build, quy tắc khi chạm API public, quy tắc thêm dependency, cách sinh migration, cách cập nhật docs, khi nào cần approval của người. Codex đọc file này trước khi làm việc, nên đây là nơi gần nhất để “khóa” behavior theo repo. citeturn11view2turn10search7

Việc thứ hai là tạo **bộ skill tối thiểu** trong `.agents/skills/`. Tôi đề xuất bắt đầu bằng ba skill, không nhiều hơn:

- **`repo-bootstrap`**: setup env, dependency, secret check, lệnh baseline health check.
- **`verify-changed-code`**: phân tích diff rồi chạy đúng stack verify của repo.
- **`pr-handoff`**: sinh summary, risks, test evidence, rollout notes.

Mỗi skill bắt buộc có `SKILL.md`; script chỉ thêm khi thật sự giúp workflow ổn định hơn. Cách này đúng với design của Codex skills: metadata có trước, full instructions chỉ load khi skill được chọn, nên bớt phình context. citeturn5view0turn12view0turn15view1

Việc thứ ba là **chuẩn hóa execution surface**. Nếu repo hiện có nhiều command shell ad-hoc, hãy gom lại thành một CLI nội bộ hoặc scripts có shape thống nhất. Với những use case lặp lại như đọc logs, query admin API, đọc SQLite, tải artifact, hay retry job, OpenAI khuyên xây CLI + companion skill. Đây chính là chỗ bạn nên diễn giải lại các note đỏ trên sơ đồ: chúng không phải Harness, mà là **tiêu chuẩn để thiết kế CLI cho Codex dùng được**. citeturn5view6

Kết quả mong muốn của giai đoạn này là: chỉ cần mở repo bằng Codex CLI, Codex đã biết cách setup, biết lúc nào phải verify, biết dùng skill nào cho task nào, và biết khi nào phải dừng để xin approval. Ngoài ra, cần khóa default permissions ở `workspace-write` + `on-request`, hoặc profile tương đương, để local autonomy vừa đủ mà chưa nguy hiểm. citeturn8view0turn8view1turn8view2

### Giai đoạn Harness runtime

Khi repo đã ổn định ở tầng Codex customization, giai đoạn thứ hai là tách riêng một thư mục `harness/` trong source code để xây runtime orchestration.

Ở giai đoạn này, tôi khuyên kiến trúc như sau:

```text
repo/
  AGENTS.md
  .agents/
    skills/
      repo-bootstrap/
      verify-changed-code/
      pr-handoff/
  harness/
    orchestrator/
    tools/
    skills_registry/
    memory/
    policies/
    hooks/
    telemetry/
    evals/
```

Trong `harness/orchestrator`, bạn triển khai runner nhận một `TaskSpec`, chọn agent role, gắn instruction bundle, chọn tools/skills tương ứng, rồi chạy qua Agents SDK. Về mặt công nghệ, nên dùng **Responses API làm nền**, và **Agents SDK** cho orchestration. Đừng khởi động mới trên Assistants vì nó không còn là hướng đi cho dự án mới và đã có lộ trình shutdown rõ ràng. citeturn16view1turn16view0

Trong `harness/tools`, chia rõ ba loại adapter:

- **Built-in OpenAI tools adapters**: web/file/code interpreter/computer use khi thực sự khớp.
- **MCP adapters**: kết nối tới dev tools, docs, browser, hoặc internal systems qua MCP.
- **CLI adapters**: wrapper cho lệnh nội bộ, luôn tách read-only và side-effecting commands. citeturn14view4turn5view2turn12view1turn12view3

Nếu mục tiêu là cho source code của bạn “làm việc với Codex” chứ không chỉ “gọi model”, thì nên thêm một adapter đặc biệt: **Codex-as-executor**. OpenAI đã có pattern chính thức cho việc đưa **Codex CLI lên làm MCP server**, nơi Agents SDK gọi vào `codex()` để bắt đầu và `codex-reply()` để tiếp tục thread. Đây là cách đẹp nhất để Harness orchestration được các task coding thực tế mà vẫn reviewable. citeturn11view0

### Giai đoạn memory, observability và policy

Sau khi orchestration chạy được, bạn mới nên đầu tư mạnh vào memory, tracing và policy. Đây là giai đoạn biến hệ thống từ “chạy được” thành “vận hành được”.

Về **memory**, tôi khuyên tách ba lớp triển khai:

- **Working memory**: conversation state bằng Responses/Conversations.
- **Task memory**: file artifacts, plans, intermediate outputs, summaries.
- **Long-term memory**: documents, runbooks, architectural decisions, postmortems, design RFCs được index bằng Retrieval/vector stores. Với long-running tasks, bật compaction để tránh ngữ cảnh bị phình mãi. citeturn16view1turn14view3turn14view1

Về **observability**, mỗi run phải có:
- structured logs,
- traces,
- skill invocation records,
- tool call outputs đã sanitize,
- approval events,
- eval score snapshots.

Tracing trong Agents SDK đã cover model calls, tool calls, handoffs, guardrails và custom spans, nên đừng phát minh lại từ đầu nếu không thật cần. citeturn5view4turn5view9

Về **policy**, tôi khuyên mã hóa ba lớp:
- **permission profile** cho filesystem/network,
- **approval policy** cho side effects,
- **guardrails** cho input/output/tool invocation.

Codex hiện có built-in permission profiles và các approval combinations rõ ràng như `read-only`, `workspace-write`, `danger-full-access`; app/CLI cũng có thể chuyển mode trong session. Harness nên dùng những boundary này làm contract mặc định, thay vì cho từng task tự xin đặc quyền tùy hứng. citeturn8view0turn8view1turn8view2

## Cấu trúc repo và hợp đồng làm việc với Codex

Đây là phần tôi cho là thiết thực nhất nếu bạn muốn bắt tay triển khai ngay trên một codebase thật.

### Bộ file nên có ngay

Bộ file tối thiểu tôi đề xuất:

```text
AGENTS.md
.agents/skills/
  repo-bootstrap/
    SKILL.md
    scripts/check_env.sh
  verify-changed-code/
    SKILL.md
    scripts/verify.sh
  pr-handoff/
    SKILL.md
docs/harness/
  architecture.md
  operational-policies.md
harness/
  orchestrator/
  tools/
  policies/
  telemetry/
  evals/
```

`AGENTS.md` nên giữ ngắn và mệnh lệnh hóa. Một định dạng tốt là:

```md
## Build and test
- Use `make test` for full verification.
- Use `make lint` before any handoff.
- If public API changes, update `docs/` and changelog.

## Safety and approvals
- Do not add new production dependencies without explicit approval.
- Ask before running commands that modify infra or secrets.
- Prefer read-only inspection before write actions.

## Repo conventions
- Keep changes minimal and localized.
- Add regression tests for bug fixes.
- Summarize file-by-file impact before final handoff.
```

Tôi không khuyến nghị để business logic sâu trong `AGENTS.md`. File này chỉ nên nắm **working agreements** và **policy triggers**. Logic chi tiết hơn nên đẩy vào skill hoặc script. Điều này khớp với guidance chính thức của Codex về `AGENTS.md` và repo-local skills. citeturn11view2turn15view1

### Ba skill nên ưu tiên trước

Skill đầu tiên nên là **`repo-bootstrap`**. Skill này lo phát hiện package manager, tạo virtualenv nếu cần, kiểm tra secrets thiếu, và sinh báo cáo “repo healthy/not healthy”. Nó nên là skill đầu tiên mà Codex kích hoạt khi vào repo mới hoặc khi workspace bị thay đổi nhiều.

Skill thứ hai nên là **`verify-changed-code`**. Đây là skill có ROI cao nhất, vì nó biến “đã sửa xong chưa?” thành một workflow chuẩn. OpenAI mô tả pattern này khá rõ trong cách họ dùng skills trong các repo Agents SDK: skill định nghĩa đúng stack verify là gì, còn `AGENTS.md` ép dùng nó ở đúng thời điểm. citeturn15view1

Skill thứ ba nên là **`pr-handoff`**. Skill này buộc mọi task kết thúc bằng: summary, phạm vi ảnh hưởng, tests đã chạy, rủi ro còn lại, và việc gì cần người review. Điều này tạo nền cho observability và improvement loop sau này.

### Khi nào cần một CLI riêng

Nếu source code của bạn liên tục phải làm một trong các việc sau, tôi khuyên tách ra thành **CLI riêng** thay vì chỉ dùng shell commands vặt:

- query một internal API,
- đọc logs/artifacts/build outputs,
- duyệt một local database,
- tải file và đọc theo ID,
- thao tác với một hệ thống cần auth/setup lặp lại,
- hay thực hiện các workflow có read/write rõ ràng.

Tài liệu “Create a CLI Codex can use” nhấn mạnh đúng các pattern này và khuyên tạo thêm **companion skill** để dạy Codex lệnh nào nên chạy trước, output nên nhỏ thế nào, file tải về nằm ở đâu, và write actions nào cần approval. Đó là ngôn ngữ triển khai chính xác hơn nhiều so với việc ghi chú chung chung “kubectl skill”, “goclaw trace read abc” ngay trong sơ đồ. citeturn5view6

Nếu bạn muốn giữ lại tinh thần của các note đỏ trong ảnh, tôi đề nghị đổi chúng thành một guideline rõ hơn như sau:

- **CLI phải có `--help` sạch và đầy đủ**.
- **Ưu tiên output JSON hoặc format ổn định khi máy cần đọc**.
- **Có command read-only tách riêng command write**.
- **Lỗi phải mô tả được nguyên nhân và hành động tiếp theo**.
- **Mọi thao tác nguy hiểm phải đi qua approval path**.

Phần này là **chuẩn thiết kế tool/skill**, không phải phần lõi của Harness.

## Cơ chế tự đánh giá và cải thiện liên tục

Đây là phần sẽ quyết định Harness của bạn chỉ “chạy được” hay thật sự “tự cải thiện được”.

OpenAI đã mô tả một improvement loop rất sát nhu cầu của bạn: bắt đầu từ **traces thật**, thêm **feedback của người** và **feedback do model sinh ra**, chuyển chúng thành **evals**, rồi tạo **Codex-ready handoff** để sửa chính Harness. Tôi cho rằng đây chính là đường triển khai tốt nhất cho yêu cầu “tự đánh giá, tự sửa sai, tự cải thiện”. citeturn15view0turn11view3

Cụ thể, bạn nên vận hành vòng lặp sau:

**Bước đầu**, mọi task của Harness đều phải phát sinh trace. Trace phải ghi ít nhất:
- prompt/instructions version,
- skill nào được load,
- tool nào được gọi,
- command nào chạy thật,
- approval nào được xin/deny,
- output cuối cùng và artifacts.

Agents SDK tracing đã hỗ trợ gần như đầy đủ những phần quan trọng nhất của cấu trúc này. citeturn5view4

**Bước tiếp theo**, lấy một sample có chủ đích gồm các run tốt, run lỗi, run gần đúng, run có side effect không cần thiết. Với mỗi trace, tạo feedback theo một schema ổn định:  
mục tiêu có đạt không,  
tool có bị gọi thừa không,  
skill có được chọn đúng không,  
response/handoff có đủ bằng chứng không,  
approval boundary có bị vượt không.

**Sau đó**, chuyển feedback thành evals. Tài liệu của OpenAI nhấn mạnh rằng evals cho skills/harness nên nhìn giống các lightweight end-to-end tests: prompt → captured run → một tập check nhỏ → score so sánh theo thời gian. Điểm quan trọng là đừng chỉ đo “answer quality”; hãy đo cả “skill invoked correctly?”, “expected commands ran?”, “output followed repo conventions?”. citeturn11view3turn5view5

**Cuối cùng**, dùng kết quả đó để thay Harness theo thứ tự ưu tiên sau:

- sửa `AGENTS.md` nếu lỗi là policy hoặc repo guidance;
- sửa `SKILL.md` nếu lỗi là workflow description;
- sửa CLI/tool adapter nếu lỗi là execution surface;
- sửa orchestration/routing nếu lỗi là chọn sai agent/tool;
- sửa guardrails/policies nếu lỗi là vượt boundary;
- sửa evals khi coverage hành vi còn thiếu.

Đây cũng chính là chỗ tôi nối lại với sơ đồ ban đầu của bạn: **Monitoring → Logs/Traces/Analytics** không nên chỉ để “quan sát”, mà phải trở thành đầu vào cho **Harness improvement loop**. Nếu không có bước này, monitoring chỉ là dashboard đẹp; còn nếu có, nó trở thành động cơ cải tiến thật. citeturn5view4turn5view5turn15view0

### Tiêu chí hoàn thành thực tế

Tôi sẽ coi việc triển khai Harness cho một source code làm việc với Codex là đạt mức “done hữu ích” khi có đủ các dấu hiệu sau:

Source code có `AGENTS.md` ở mức repo, có ít nhất ba repo-local skills hoạt động, và Codex khi mở repo mới có thể tự bootstrap, tự verify thay đổi và tự sinh handoff reviewable mà không cần prompt dài lặp đi lặp lại. citeturn11view2turn12view0turn15view1

Mọi thao tác local đều đi qua permission boundary rõ ràng, tối thiểu ở mức `workspace-write` + `on-request`, hoặc permission profile tương đương, để tránh biến “automation” thành “toàn quyền mặc định”. citeturn8view0turn8view1turn8view2

Nếu có orchestration layer riêng, nó chạy trên Responses/Agents SDK, không dựa vào Assistants API cho thiết kế mới; Codex CLI có thể được gắn làm MCP server khi cần coding workflows nhiều bước, và traces/evals đủ để tái tạo các failure mode quan trọng. citeturn16view1turn16view0turn11view0turn5view4turn5view5

Và quan trọng nhất: phần **kubectl/goclaw/examples** đã được di chuyển ra khỏi lõi Harness, giữ đúng vai trò là **mẫu skill/CLI standard**, không còn làm nhiễu kiến trúc trung tâm.

Với tất cả những điểm trên, bản kết luận ngắn gọn của tôi là: **sơ đồ của bạn có hướng đúng, nhưng cần tách rất dứt khoát giữa “hệ điều phối Harness” và “ví dụ skill/CLI cho Codex”**. Kiến trúc đúng cho 2026 là **Repo Guidance + Repo-local Skills + Agent-friendly CLI** ở tầng nền, sau đó tiến lên **Responses API + Agents SDK + Codex MCP + Tracing/Evals** ở tầng orchestration. Đó là con đường vừa khớp với cách Codex vận hành hiện nay, vừa đủ chắc để bạn tự đánh giá, tự sửa sai và cải thiện Harness theo vòng lặp có bằng chứng. citeturn5view3turn11view0turn11view2turn12view0turn15view0turn16view1