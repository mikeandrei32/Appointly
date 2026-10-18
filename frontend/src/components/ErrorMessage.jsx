export default function ErrorMessage({ children }) {
  return children ? <p role="alert" className="rounded-xl border border-red-200 bg-red-50 p-4 text-sm leading-6 text-red-800">{children}</p> : null
}
