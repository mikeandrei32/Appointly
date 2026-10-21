import { ArrowRight, CalendarDays, CalendarCheck2, Clock3, Heart, MoveUpRight } from 'lucide-react'
import { Link } from 'react-router-dom'
import Brand from '../components/Brand'
import { useAuth } from '../hooks/useAuth'

const benefits = [
  { icon: CalendarDays, title: 'A time that suits you', text: 'Choose from available clinic appointments and plan ahead.' },
  { icon: CalendarCheck2, title: 'Your visits, in one place', text: 'Keep your upcoming appointments and past visits close at hand.' },
  { icon: Clock3, title: 'Room for a change of plans', text: 'Cancel an upcoming visit when you need to make other arrangements.' },
]

export default function FoundationPage() {
  const { user, loading } = useAuth()
  const isAdmin = user?.roles.includes('Admin')
  const accountPath = isAdmin ? '/admin' : '/dashboard'
  const primaryPath = user ? (isAdmin ? '/admin' : '/book') : '/register'
  const primaryLabel = isAdmin ? 'Manage appointments' : 'Book a visit'

  return (
    <div className="min-h-screen bg-[#fafbf8] text-slate-900">
      <header className="border-b border-slate-200/70 bg-white">
        <nav aria-label="Main navigation" className="mx-auto flex max-w-6xl flex-wrap items-center justify-between gap-4 px-6 py-6">
          <Brand />
          {!loading && <Link to={user ? accountPath : '/login'}
            className="inline-flex items-center gap-2 rounded-full border border-slate-300 px-5 py-2.5 text-sm font-semibold transition hover:border-blue-600 hover:text-blue-700">
            {user ? 'My account' : 'Sign in'} <MoveUpRight size={15} aria-hidden="true" />
          </Link>}
        </nav>
      </header>

      <main>
        <section className="mx-auto grid max-w-6xl items-center gap-12 px-6 py-14 md:py-20 lg:grid-cols-[1.1fr_1fr] lg:gap-16" aria-labelledby="welcome-heading">
          <div className="min-w-0">
            <p className="mb-6 inline-flex items-center gap-2 text-xs font-bold uppercase tracking-[0.18em] text-blue-700">
              <span className="h-2 w-2 rounded-full bg-blue-600" aria-hidden="true" /> A little planning. More peace of mind.
            </p>
            <h1 id="welcome-heading" className="max-w-xl text-5xl font-semibold leading-[1.08] tracking-[-0.045em] sm:text-6xl lg:text-7xl">
              Your next visit.<br /><span className="text-blue-600">Made simple.</span>
            </h1>
            <p className="mt-7 max-w-md text-lg leading-8 text-slate-600">
              Make time for your wellbeing. Find a clinic appointment that fits your day and keep your visits organised.
            </p>
            <div className="mt-9 flex flex-wrap items-center gap-5">
              {!loading && <Link to={primaryPath}
                className="inline-flex items-center justify-center gap-4 rounded-full bg-blue-600 px-7 py-4 font-semibold text-white shadow-lg shadow-blue-600/15 transition hover:bg-blue-700">
                {primaryLabel} <ArrowRight size={19} aria-hidden="true" />
              </Link>}
              {!user && !loading && <span className="text-sm text-slate-500">New here? Start with an account.</span>}
            </div>
            <div className="mt-10 flex items-center gap-3 border-t border-slate-200 pt-6 text-sm text-slate-600">
              <Heart size={18} className="shrink-0 text-blue-600" aria-hidden="true" />
              A calmer way to plan your clinic visit.
            </div>
          </div>

          <div className="relative min-w-0 overflow-hidden rounded-[2rem] bg-[#e8eefb] p-7 sm:p-10">
            <div className="absolute -right-16 -top-16 h-60 w-60 rounded-full border-[40px] border-white/35" aria-hidden="true" />
            <div className="relative flex items-center justify-between">
              <span className="rounded-full bg-white/80 px-4 py-2 text-xs font-semibold tracking-wide text-blue-900">A moment for you</span>
              <Heart size={23} className="text-blue-700" strokeWidth={1.5} aria-hidden="true" />
            </div>
            <div aria-hidden="true" className="relative mx-auto mb-9 mt-10 max-w-72 rotate-[-4deg] rounded-2xl bg-white p-6 shadow-xl shadow-blue-950/10">
              <div className="mb-6 flex items-center justify-between border-b border-slate-100 pb-4">
                <div className="h-2.5 w-20 rounded-full bg-blue-100" />
                <CalendarDays size={21} className="text-blue-600" />
              </div>
              <div className="grid grid-cols-5 gap-3">
                {Array.from({ length: 15 }, (_, index) => <div key={index}
                  className={`flex aspect-square items-center justify-center rounded-lg ${index === 7 ? 'bg-blue-600 text-white shadow-md shadow-blue-600/20' : 'bg-slate-50'}`}>
                  {index === 7 ? <Heart size={20} fill="currentColor" /> : <span className="h-1.5 w-1.5 rounded-full bg-slate-200" />}
                </div>)}
              </div>
              <div className="mt-5 flex items-center gap-2"><span className="h-2 w-2 rounded-full bg-blue-300" /><span className="h-2 w-24 rounded-full bg-slate-100" /></div>
            </div>
            <div className="relative border-t border-blue-200/70 pt-6">
              <h2 className="text-2xl font-semibold tracking-tight text-blue-950">Less to organise.<br />More time for you.</h2>
              <p className="mt-3 max-w-xs text-sm leading-6 text-slate-600">A place for your appointments, so you can get on with your day.</p>
            </div>
          </div>
        </section>

        <section aria-label="Plan your visit with Appointly" className="border-y border-slate-200/70 bg-white">
          <div className="mx-auto grid max-w-6xl gap-9 px-6 py-10 md:grid-cols-3 md:gap-10 md:py-12">
            {benefits.map(({ icon: Icon, title, text }) => <article key={title}>
              <Icon size={23} strokeWidth={1.6} className="mb-4 text-blue-600" aria-hidden="true" />
              <h2 className="text-base font-semibold">{title}</h2>
              <p className="mt-2 text-sm leading-6 text-slate-500">{text}</p>
            </article>)}
          </div>
        </section>
      </main>

      <footer className="mx-auto flex max-w-6xl flex-wrap items-center justify-between gap-3 px-6 py-7 text-xs text-slate-500">
        <p>Appointly · Make time for your wellbeing.</p>
        <p>Demo only. Please use fictional information.</p>
      </footer>
    </div>
  )
}
