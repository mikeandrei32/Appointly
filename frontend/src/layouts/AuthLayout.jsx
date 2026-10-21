import Brand from '../components/Brand'

export default function AuthLayout({ title, description, children }) {
  return <main className="mx-auto min-h-screen max-w-md px-6 py-10 sm:py-16">
    <Brand />
    <section className="mt-10 rounded-2xl border border-slate-200 bg-white p-6 shadow-sm sm:p-8">
      <h1 className="text-2xl font-bold tracking-tight">{title}</h1>
      <p className="mb-7 mt-2 text-sm leading-6 text-slate-600">{description}</p>
      {children}
    </section>
  </main>
}
