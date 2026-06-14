import { Outlet } from 'react-router-dom'

export function AuthLayout() {
  return (
    <div className="flex min-h-screen items-center justify-center px-4 py-12">
      <div className="surface-panel w-full max-w-md p-6">
        <Outlet />
      </div>
    </div>
  )
}
