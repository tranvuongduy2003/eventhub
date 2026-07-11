---
name: zustand-web
description: Zustand state management patterns for web/. Use when creating stores, managing global state, persisting state to sessionStorage, or working with files in stores/ directories. Triggers on store creation, state management tasks, Zustand hooks, or when user asks about global state in web/.
---

# Zustand

Solution web patterns for Zustand state management. Always use selectors and follow naming conventions.

## Quick Reference

| Item              | File Convention | Code Convention                       |
| ----------------- | --------------- | ------------------------------------- |
| Store file        | `use*Store.ts`  | `useCamelCaseStore`                   |
| State interface   | -               | `{Feature}State`                      |
| Actions interface | -               | `{Feature}Actions`                    |
| Clear action      | -               | `clear{Feature}Store` or `clearStore` |

## Critical Rules

1. **Always use selectors** - Never destructure store hooks
2. **NO type assertions** - No `as` keyword, non-null assertions, or angle bracket casts
3. **Use `@/` imports** - Never relative imports
4. **Named exports only** - No default exports for new stores
5. **Arrow function components** - `const X = () => {}`

## Store Patterns

### Simple Store

For simple UI state without persistence:

```typescript
import { create } from "zustand";

type State = {
  value: string | null;
  isOpen: boolean;
};

type Actions = {
  setValue: (value: string) => void;
  reset: () => void;
};

const initialState: State = {
  value: null,
  isOpen: false,
};

export const useFeatureStore = create<State & Actions>((set) => ({
  ...initialState,
  setValue: (value) => set({ value }),
  reset: () => set(initialState),
}));
```

### Persisted Store

For stores that need sessionStorage persistence and DevTools:

```typescript
import { create } from "zustand";
import { createJSONStorage, devtools, persist } from "zustand/middleware";

interface RecordFilterState {
  status: string;
  dateRange: [string, string] | null;
}

interface RecordFilterActions {
  setStatus: (status: string) => void;
  setDateRange: (range: [string, string] | null) => void;
  clearStore: VoidFunction;
}

const initialState: RecordFilterState = {
  status: "",
  dateRange: null,
};

export const useRecordFilterStore = create<
  RecordFilterState & RecordFilterActions
>()(
  devtools(
    persist(
      (set) => ({
        ...initialState,
        setStatus: (status) => set({ status }),
        setDateRange: (dateRange) => set({ dateRange }),
        clearStore: () => set(initialState),
      }),
      {
        name: "recordFilterStore",
        storage: createJSONStorage(() => sessionStorage),
      },
    ),
  ),
);
```

### Context-Based Store (Multi-Instance)

For stores that need multiple instances (e.g., per-item). See [patterns-context-store.md](references/patterns-context-store.md).

## Usage Patterns

### Selecting State (Correct)

```typescript
// Select individual values
const status = useRecordFilterStore((state) => state.status);
const dateRange = useRecordFilterStore((state) => state.dateRange);
const setStatus = useRecordFilterStore((state) => state.setStatus);
```

### Selecting State (WRONG - Never Do This)

```typescript
// NEVER destructure the store hook
const { status, dateRange, setStatus } = useRecordFilterStore(); // WRONG
```

### Direct Store Access (Outside Components)

```typescript
// Access store outside React components
useRecordFilterStore.getState().setStatus("pending");
const currentStatus = useRecordFilterStore.getState().status;
```

## Middleware Stack

```typescript
create<StoreType>()(
  devtools(
    // Outer: Redux DevTools integration
    persist(
      // Inner: sessionStorage persistence
      (set, get) => ({
        // store implementation
      }),
      {
        name: "storeName", // Required: unique key
        storage: createJSONStorage(() => sessionStorage), // sessionStorage, not localStorage
      },
    ),
  ),
);
```

## File Locations

| Type           | Location                             |
| -------------- | ------------------------------------ |
| Global stores  | `web/src/stores/`                    |
| Feature stores | `web/src/features/[feature]/stores/` |

## Detailed Patterns

- [Context-Based Store](references/patterns-context-store.md) - Multi-instance stores with React Context
- [Advanced Patterns](references/patterns-advanced.md) - Slices, computed values, subscriptions
