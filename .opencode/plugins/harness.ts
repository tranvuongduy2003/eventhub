import type { Plugin } from "@opencode-ai/plugin";
import { join } from "node:path";

type HookResult = {
  exitCode: number;
  json: Record<string, unknown> | null;
  stdout: string;
};

const TOOL_MAP: Record<string, string> = {
  write: "Write",
  edit: "Write",
  bash: "Shell",
  task: "Task",
};

const GUARDED_TOOLS = new Set(["write", "edit", "bash", "task"]);

function projectRoot(directory: string, worktree?: string): string {
  return worktree ?? directory;
}

async function invokeHook(
  $: Parameters<Plugin>[0]["$"],
  scriptPath: string,
  input: unknown,
  root: string,
): Promise<HookResult> {
  const inputJson = JSON.stringify(input).replace(/'/g, "''");
  const result =
    await $`pwsh -NoProfile -File ${scriptPath} -InputJson '${inputJson}'`
      .env({
        ...process.env,
        OPENCODE_PROJECT_DIR: root,
        CURSOR_PROJECT_DIR: root,
      })
      .quiet()
      .nothrow();

  const stdout = result.stdout.toString().trim();
  let json: Record<string, unknown> | null = null;
  if (stdout) {
    try {
      json = JSON.parse(stdout) as Record<string, unknown>;
    } catch {
      json = null;
    }
  }

  return { exitCode: result.exitCode, json, stdout };
}

function cursorToolInput(
  tool: string,
  args: Record<string, unknown>,
): Record<string, unknown> {
  if (tool === "bash") {
    return { command: args.command ?? args.cmd ?? "" };
  }

  const filePath =
    args.file_path ?? args.filePath ?? args.path ?? args.file ?? "";

  return { file_path: filePath, ...args };
}

export const HarnessPlugin: Plugin = async ({ $, directory, worktree }) => {
  const root = projectRoot(directory, worktree);
  const hooksDir = join(root, ".opencode", "hooks");

  const preToolGuard = join(hooksDir, "pre-tool-guard.ps1");
  const beforeShellGuard = join(hooksDir, "before-shell-guard.ps1");
  const postEditVerify = join(hooksDir, "post-edit-verify.ps1");
  const stopGate = join(hooksDir, "stop-gate.ps1");
  const preCompactBackup = join(hooksDir, "pre-compact-backup.ps1");

  return {
    "tool.execute.before": async (input, output) => {
      const tool = input.tool;
      if (!GUARDED_TOOLS.has(tool)) {
        return;
      }

      const cursorName = TOOL_MAP[tool] ?? tool;
      const hookInput = {
        tool_name: cursorName,
        tool_input: cursorToolInput(
          tool,
          (output.args ?? {}) as Record<string, unknown>,
        ),
        hook_event_name: "preToolUse",
      };

      const result = await invokeHook($, preToolGuard, hookInput, root);
      if (result.exitCode === 2 && result.json?.permission === "deny") {
        const message =
          (result.json.agent_message as string) ??
          (result.json.user_message as string) ??
          "Blocked by agent harness.";
        throw new Error(message);
      }

      if (tool === "bash") {
        const shellInput = {
          command: (output.args as Record<string, unknown>)?.command ?? "",
          hook_event_name: "beforeShellExecution",
        };
        const shellResult = await invokeHook(
          $,
          beforeShellGuard,
          shellInput,
          root,
        );
        if (
          shellResult.exitCode === 2 &&
          shellResult.json?.permission === "deny"
        ) {
          const message =
            (shellResult.json.agent_message as string) ??
            (shellResult.json.user_message as string) ??
            "Shell command blocked by agent harness.";
          throw new Error(message);
        }
      }
    },

    "file.edited": async (event) => {
      const filePath = (event as { path?: string }).path ?? "";
      await invokeHook($, postEditVerify, { file_path: filePath }, root);
    },

    "session.idle": async () => {
      const result = await invokeHook(
        $,
        stopGate,
        { status: "completed", hook_event_name: "stop" },
        root,
      );
      const followup = result.json?.followup_message;
      if (typeof followup === "string" && followup.length > 0) {
        throw new Error(followup);
      }
    },

    "experimental.session.compacting": async (input) => {
      const sessionInput = {
        conversation_id:
          (input as { sessionID?: string }).sessionID ?? "unknown",
        trigger: "compaction",
        hook_event_name: "preCompact",
      };
      await invokeHook($, preCompactBackup, sessionInput, root);
    },
  };
};
