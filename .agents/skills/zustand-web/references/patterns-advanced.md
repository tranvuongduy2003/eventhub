# Advanced Zustand Patterns

## Slices Pattern

For large stores, split into modular slices:

```typescript
import { create, StateCreator } from "zustand";
import { createJSONStorage, devtools, persist } from "zustand/middleware";

// Slice interfaces
interface UserSlice {
  user: User | null;
  setUser: (user: User) => void;
  clearUser: () => void;
}

interface SettingsSlice {
  theme: "light" | "dark";
  setTheme: (theme: "light" | "dark") => void;
}

// Combined store type
type BoundStore = UserSlice & SettingsSlice;

// Create individual slices
const createUserSlice: StateCreator<BoundStore, [], [], UserSlice> = (set) => ({
  user: null,
  setUser: (user) => set({ user }),
  clearUser: () => set({ user: null }),
});

const createSettingsSlice: StateCreator<BoundStore, [], [], SettingsSlice> = (
  set,
) => ({
  theme: "light",
  setTheme: (theme) => set({ theme }),
});

// Combine slices into one store
const useBoundStore = create<BoundStore>()(
  devtools(
    persist(
      (...a) => ({
        ...createUserSlice(...a),
        ...createSettingsSlice(...a),
      }),
      { name: "boundStore", storage: createJSONStorage(() => sessionStorage) },
    ),
  ),
);

export { useBoundStore };
```

## Subscriptions (Outside React)

Subscribe to store changes outside React components:

```typescript
// Subscribe to all changes
const unsubscribe = useFeatureStore.subscribe((state, prevState) => {
  console.log("State changed:", state);
});

// Subscribe with selector (only fires when selected value changes)
const unsubscribe = useFeatureStore.subscribe(
  (state) => state.count,
  (count, prevCount) => {
    console.log("Count changed from", prevCount, "to", count);
  },
);

// Cleanup
unsubscribe();
```

## Derived/Computed State

Compute derived values in selectors (not in store):

```typescript
// In component - compute derived state in selector
const doubledCount = useFeatureStore((state) => state.count * 2);

const filteredItems = useFeatureStore((state) =>
  state.items.filter((item) => item.active),
);
```

## Async Actions

Handle async operations in actions:

```typescript
interface AsyncStore {
  data: Data | null;
  isLoading: boolean;
  error: string | null;
  fetchData: () => Promise<void>;
}

const useAsyncStore = create<AsyncStore>()(
  devtools((set) => ({
    data: null,
    isLoading: false,
    error: null,
    fetchData: async () => {
      set({ isLoading: true, error: null });
      try {
        const response = await fetch("/api/data");
        const data = await response.json();
        set({ data, isLoading: false });
      } catch (error) {
        set({ error: "Failed to fetch", isLoading: false });
      }
    },
  })),
);
```

## Transient Updates (No Re-render)

For high-frequency updates that shouldn't trigger re-renders:

```typescript
interface TransientStore {
  position: { x: number; y: number };
  // Transient state (not persisted, not triggering re-renders)
  _transient: {
    mousePosition: { x: number; y: number };
  };
}

// Update transient state without triggering subscribers
useStore.setState(
  (state) => ({
    _transient: { ...state._transient, mousePosition: { x, y } },
  }),
  true,
); // true = replace instead of merge
```

## Reset Store on Logout

Pattern for cleaning up stores on user logout:

```typescript
// In store
const initialState: IFeatureStore = {
  value: "",
  count: 0,
};

const useFeatureStore = create<IFeatureStore & FeatureStoreActions>()(
  devtools(
    persist(
      (set) => ({
        ...initialState,
        // Reset action
        reset: () => set(initialState),
      }),
      {
        name: "featureStore",
        storage: createJSONStorage(() => sessionStorage),
      },
    ),
  ),
);

// In logout handler
const handleLogout = () => {
  useFeatureStore.getState().reset();
  useOtherStore.getState().reset();
  // ... reset all stores
};
```
