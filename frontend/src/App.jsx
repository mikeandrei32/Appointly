import { BrowserRouter, Link, Route, Routes } from 'react-router-dom'
import { AuthProvider } from './context/AuthProvider'
import RequireAuth from './components/RequireAuth'
import AccountLayout from './layouts/AccountLayout'
import FoundationPage from './pages/FoundationPage'
import LoginPage from './pages/LoginPage'
import RegisterPage from './pages/RegisterPage'
import AccountPage from './pages/AccountPage'
import AdminDashboardPage from './pages/AdminDashboardPage'
import AdminAppointmentsPage from './pages/AdminAppointmentsPage'
import AdminSchedulePage from './pages/AdminSchedulePage'
import BookAppointmentPage from './pages/BookAppointmentPage'
import MyAppointmentsPage from './pages/MyAppointmentsPage'
import PatientDashboardPage from './pages/PatientDashboardPage'

export default function App() {
  return <BrowserRouter><AuthProvider><Routes>
    <Route path="/" element={<FoundationPage />} />
    <Route path="/login" element={<LoginPage />} />
    <Route path="/register" element={<RegisterPage />} />
    <Route element={<RequireAuth />}>
      <Route element={<AccountLayout />}>
        <Route path="/account" element={<AccountPage />} />
        <Route element={<RequireAuth role="Patient" />}>
          <Route path="/dashboard" element={<PatientDashboardPage />} />
          <Route path="/book" element={<BookAppointmentPage />} />
          <Route path="/appointments" element={<MyAppointmentsPage key="upcoming" />} />
          <Route path="/appointments/history" element={<MyAppointmentsPage key="history" history />} />
        </Route>
        <Route element={<RequireAuth role="Admin" />}>
          <Route path="/admin" element={<AdminDashboardPage />} />
          <Route path="/admin/appointments" element={<AdminAppointmentsPage />} />
          <Route path="/admin/schedule" element={<AdminSchedulePage />} />
        </Route>
      </Route>
    </Route>
    <Route path="*" element={<main className="p-10"><h1 className="mb-4 text-2xl font-bold">Page not found</h1><Link to="/" className="text-blue-700 underline">Return to Appointly</Link></main>} />
  </Routes></AuthProvider></BrowserRouter>
}
