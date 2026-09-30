import { useEffect, useState } from 'react'
import { session } from './api'
import LoginPage from './components/LoginPage'
import Dashboard from './components/Dashboard'
import ToastProvider from './components/ToastProvider'
import { useToast } from './components/toast-context'
import './App.css'

function AppContent() {
  const [loggedIn, setLoggedIn] = useState(() => Boolean(session.getToken()))
  const toast = useToast()

  // The API client fires this event on any 401.
  useEffect(() => {
    const onLogout = () => {
      setLoggedIn(false)
      toast.info('Your session has expired. Please sign in again.')
    }
    window.addEventListener('auth:logout', onLogout)
    return () => window.removeEventListener('auth:logout', onLogout)
  }, [toast])

  function handleLogout() {
    session.clear()
    setLoggedIn(false)
  }

  return loggedIn ? <Dashboard onLogout={handleLogout} /> : <LoginPage onLoggedIn={() => setLoggedIn(true)} />
}

export default function App() {
  return (
    <ToastProvider>
      <AppContent />
    </ToastProvider>
  )
}
