# Icons

**Always use the project's configured `iconLibrary` for imports.** Check the `iconLibrary` field from project context: `lucide` â†’ `lucide-react`, `tabler` â†’ `@tabler/icons-react`, etc. Never assume `lucide-react`.

---

## Icons in Button use data-icon attribute

Add `data-icon="inline-start"` (prefix) or `data-icon="inline-end"` (suffix) to the icon. No sizing classes on the icon.

**Incorrect:**

```tsx
<Button>
  <SearchIcon className="mr-2 size-4" />
  Search
</Button>
```

**Correct:**

```tsx
<Button>
  <SearchIcon data-icon="inline-start"/>
  Search
</Button>

<Button>
  Next
  <ArrowRightIcon data-icon="inline-end"/>
</Button>
```

---

## No sizing classes on icons inside components

Components handle icon sizing via CSS. Don't add `size-4`, `w-4 h-4`, or other sizing classes to icons inside `Button`, `DropdownMenuItem`, `Alert`, `Sidebar*`, or other shadcn components. Unless the user explicitly asks for custom icon sizes.

**Incorrect:**

```tsx
<Button>
  <SearchIcon className="size-4" data-icon="inline-start" />
  Search
</Button>

<DropdownMenuItem>
  <SettingsIcon className="mr-2 size-4" />
  Settings
</DropdownMenuItem>
```

**Correct:**

```tsx
<Button>
  <SearchIcon data-icon="inline-start" />
  Search
</Button>

<DropdownMenuItem>
  <SettingsIcon />
  Settings
</DropdownMenuItem>
```

---

## No Unicode text characters as visual markers â€” use an icon component

Never render Unicode symbols (`â€¢`, `Â·`, `â†’`, `â†`, `â†‘`, `â†“`, `â€º`, `Â»`, `â€¹`, `Â«`, `âœ“`, `âœ—`, `Ã—`, `â˜…`, `â˜†`, `âš `, `â„¹`, etc.) as JSX text when they are being used as **visual markers, separators, bullets, arrows, or indicators**. Import the equivalent component from the project's configured `iconLibrary` instead.

This applies to every context where the character carries visual meaning (bullet between metadata, arrow in a CTA, checkmark in a status row, chevron in a breadcrumb separator). It does *not* apply to real punctuation inside prose (em-dash `â€”`, en-dash `â€“`, ellipsis `â€¦` when it ends a sentence, apostrophes, etc.).

**Incorrect:**

```tsx
<div className="flex items-center gap-2 text-xs">
  <span>{user.name}</span>
  <span>â€¢</span>                              {/* bullet as text */}
  <span>{formatDateTime(item.occurredAt)}</span>
</div>

<Button>
  Next â†’                                      {/* arrow as text */}
</Button>

<div className="flex items-center gap-1">
  <span className="text-emerald-600">âœ“</span>  {/* checkmark as text */}
  <span>Paid</span>
</div>

<nav className="flex gap-1">
  <span>Home</span>
  <span>â€º</span>                              {/* chevron as text */}
  <span>Records</span>
</nav>
```

**Correct:**

```tsx
import { Dot, ArrowRight, Check, ChevronRight } from "lucide-react"

<div className="flex items-center gap-2 text-xs">
  <span>{user.name}</span>
  <Dot className="text-muted-foreground/50" strokeWidth={4} />
  <span>{formatDateTime(item.occurredAt)}</span>
</div>

<Button>
  Next
  <ArrowRight data-icon="inline-end" />
</Button>

<div className="flex items-center gap-1">
  <Check />
  <span>Paid</span>
</div>

<Breadcrumb>
  <BreadcrumbList>
    <BreadcrumbItem><BreadcrumbLink>Home</BreadcrumbLink></BreadcrumbItem>
    <BreadcrumbSeparator /> {/* uses ChevronRight internally */}
    <BreadcrumbItem><BreadcrumbPage>Records</BreadcrumbPage></BreadcrumbItem>
  </BreadcrumbList>
</Breadcrumb>
```

### Unicode â†’ lucide-react lookup

| Unicode | Typical use | Component |
|---|---|---|
| `â€¢` `Â·` | bullet, separator between metadata | `<Dot />` (add `strokeWidth={4}` for a denser dot) |
| `â†’` `â‡’` `â‡¨` | direction, CTA suffix | `<ArrowRight />` |
| `â†` | back, return | `<ArrowLeft />` |
| `â†‘` | up, ascending | `<ArrowUp />` |
| `â†“` | down, descending | `<ArrowDown />` |
| `â€º` `Â»` | chevron, breadcrumb separator | `<ChevronRight />` (or use `<BreadcrumbSeparator>`) |
| `â€¹` `Â«` | chevron back | `<ChevronLeft />` |
| `âœ“` | checkmark, success | `<Check />` |
| `âœ—` `Ã—` `âœ•` | dismiss, fail, close | `<X />` |
| `â˜…` | filled star | `<Star className="fill-current" />` |
| `â˜†` | outline star | `<Star />` |
| `âš ` | warning | `<TriangleAlert />` |
| `â„¹` | info | `<Info />` |
| `â“` | help, question | `<CircleHelp />` |
| `â€¦` (as menu/overflow trigger) | ellipsis as indicator | `<Ellipsis />` â€” *not* when it's real sentence punctuation |
| `ðŸ”` `ðŸ”Ž` | search | `<Search />` |
| `ðŸ—‘` `ðŸ—‘ï¸` | delete | `<Trash2 />` |
| `âœï¸` | edit | `<Pencil />` |
| `âš™` `âš™ï¸` | settings | `<Settings />` |
| `ðŸ””` | notification | `<Bell />` |

### Audit

Grep the working files for stray visual markers. Restrict to `.tsx` so i18n JSON (which legitimately contains prose punctuation) is not flagged:

```
pattern: >\s*[â€¢Â·â†’â†â†‘â†“âœ“âœ—Ã—â˜…â˜†â€ºÂ»â€¹Â«â‡’â‡¨âš â„¹]+\s*<
glob:    *.tsx
```

Each hit should become an icon component (or, rarely, be justified as actual prose).

---

## Pass icons as component objects, not string keys

Use `icon={CheckIcon}`, not a string key to a lookup map.

**Incorrect:**

```tsx
const iconMap = {
  check: CheckIcon,
  alert: AlertIcon,
}

function StatusBadge({ icon }: { icon: string }) {
  const Icon = iconMap[icon]
  return <Icon />
}

<StatusBadge icon="check" />
```

**Correct:**

```tsx
// Import from the project's configured iconLibrary (e.g. lucide-react, @tabler/icons-react).
import { CheckIcon } from "lucide-react"

function StatusBadge({ icon: Icon }: { icon: React.ComponentType }) {
  return <Icon />
}

<StatusBadge icon={CheckIcon} />
```






