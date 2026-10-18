import { BrowserRouter, Link, Route, Routes } from 'react-router-dom'
import FoundationPage from './pages/FoundationPage'
export default function App() {
  return <BrowserRouter><Routes>
    <Route path="/" element={<FoundationPage />} />
    <Route path="*" element={<main className="p-10"><h1 className="mb-4 text-2xl font-bold">Page not found</h1><Link to="/" className="text-blue-700 underline">Return to Appointly</Link></main>} />
  </Routes></BrowserRouter>
}
