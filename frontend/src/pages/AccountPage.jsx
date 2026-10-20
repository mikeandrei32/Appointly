import { useAuth } from '../hooks/useAuth'

export default function AccountPage() {
  const { user } = useAuth()
  return <>
    <div><p className="text-sm font-semibold text-blue-700">Your account</p><h1 className="mt-2 text-3xl font-bold">Welcome, {user.fullName}</h1></div>
    <section className="rounded-2xl border border-slate-200 bg-white p-6">
      <h2 className="text-lg font-semibold">Profile</h2>
      <dl className="mt-4 grid gap-5 sm:grid-cols-2">
        <div><dt className="text-sm text-slate-500">Full name</dt><dd className="mt-1 break-words">{user.fullName}</dd></div>
        <div><dt className="text-sm text-slate-500">Email</dt><dd className="mt-1 break-all">{user.email}</dd></div>
        <div><dt className="text-sm text-slate-500">Account role</dt><dd className="mt-1">{user.roles.join(', ')}</dd></div>
      </dl>
    </section>
    <p className="text-sm text-slate-600">{user.roles.includes('Patient') ? 'Your account is ready. Use Book appointment to choose an available clinic time.' : 'Use Administration to verify your administrator access.'}</p>
  </>
}

