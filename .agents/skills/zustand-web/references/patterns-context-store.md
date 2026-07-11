# Context-Based Store Pattern

Use this pattern when a store needs multiple independent instances (e.g., per-thread, per-chat, per-item settings).

## When to Use

- Multiple components need their own isolated store instance
- Store state is scoped to a specific context (thread, item, session)
- Different parts of the app need separate instances of the same store logic

## Implementation

```typescript
import { createContext, ReactNode, useContext, useRef } from "react"
import { create, useStore } from "zustand"
import { createJSONStorage, persist } from "zustand/middleware"

// State interface
interface FeatureState {
  settings: Record<string, SettingsValue>
  setSettings: (id: string, settings: SettingsValue) => void
  initializeSettings: (id: string, defaults: SettingsValue) => void
}

// Factory function to create store instances
const createFeatureStore = () => {
  return create<FeatureState>()(
    persist(
      (set, get) => ({
        settings: {},
        setSettings: (id, newSettings) => {
          set({ settings: { ...get().settings, [id]: newSettings } })
        },
        initializeSettings: (id, defaults) => {
          const existing = get().settings[id]
          if (!existing) {
            set({ settings: { ...get().settings, [id]: defaults } })
          }
        },
      }),
      { name: "featureStore", storage: createJSONStorage(() => sessionStorage) },
    ),
  )
}

// Store type derived from factory
type FeatureStore = ReturnType<typeof createFeatureStore>

// Context for the store instance
const FeatureStoreContext = createContext<FeatureStore | null>(null)

// Provider component
const FeatureProvider = ({ children }: { children: ReactNode }) => {
  const storeRef = useRef<FeatureStore>(createFeatureStore())

  return (
    <FeatureStoreContext.Provider value={storeRef.current}>
      {children}
    </FeatureStoreContext.Provider>
  )
}

// Hook to access store with selector
function useFeatureContext<T>(selector: (state: FeatureState) => T): T {
  const store = useContext(FeatureStoreContext)
  if (!store) {
    throw new Error("Missing FeatureStoreContext.Provider in the tree")
  }

  return useStore(store, selector)
}

export { createFeatureStore, FeatureProvider, useFeatureContext }
```

## Usage

### Setup Provider

```tsx
// In parent component or route
import { FeatureProvider } from "@/features/feature/stores/useFeatureStore";

const FeaturePage = () => {
  return (
    <FeatureProvider>
      <FeatureContent />
    </FeatureProvider>
  );
};
```

### Consume Store

```tsx
import { useFeatureContext } from "@/features/feature/stores/useFeatureStore";

const FeatureContent = () => {
  // Always use selector function
  const settings = useFeatureContext((state) => state.settings);
  const setSettings = useFeatureContext((state) => state.setSettings);

  return (
    <button onClick={() => setSettings("item-1", { enabled: true })}>
      Update Settings
    </button>
  );
};
```

## Key Differences from Basic Store

| Aspect   | Basic Store                | Context-Based Store               |
| -------- | -------------------------- | --------------------------------- |
| Instance | Singleton                  | Multiple per Provider             |
| Access   | `useStore((s) => s.value)` | `useStoreContext((s) => s.value)` |
| Creation | `create<T>()`              | Factory function + Context        |
| Scope    | Global                     | Scoped to Provider tree           |
