export default function Button({ children, className = '', ...props }) {
  return <button className={`inline-flex items-center justify-center rounded-xl bg-blue-600 px-5 py-3 font-semibold text-white hover:bg-blue-700 disabled:cursor-not-allowed disabled:opacity-60 ${className}`} {...props}>{children}</button>
}
