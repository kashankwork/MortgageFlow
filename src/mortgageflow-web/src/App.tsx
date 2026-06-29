import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { AuthProvider } from './auth/AuthContext'
import { ProtectedRoute } from './auth/ProtectedRoute'
import { AppShell } from './components/AppShell'
import { LoginPage } from './features/auth/LoginPage'
import { LoanDetailsPage } from './features/loans/LoanDetailsPage'
import { LoanFormPage } from './features/loans/LoanFormPage'
import { LoanListPage } from './features/loans/LoanListPage'
import { QueuePage } from './features/queues/QueuePages'
import './App.css'

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: false,
      staleTime: 15_000,
    },
  },
})

export function AppRoutes() {
  return (
    <Routes>
      <Route element={<AppShell />}>
        <Route path="/login" element={<LoginPage />} />
        <Route element={<ProtectedRoute />}>
          <Route index element={<Navigate to="/loans" replace />} />
          <Route path="/loans" element={<LoanListPage />} />
          <Route path="/loans/:id" element={<LoanDetailsPage />} />
        </Route>
        <Route element={<ProtectedRoute roles={['Broker']} />}>
          <Route path="/loans/new" element={<LoanFormPage />} />
          <Route path="/loans/:id/edit" element={<LoanFormPage />} />
        </Route>
        <Route element={<ProtectedRoute roles={['Processor', 'Underwriter']} />}>
          <Route path="/queues/me" element={<QueuePage scope="me" />} />
        </Route>
        <Route element={<ProtectedRoute roles={['TeamLead']} />}>
          <Route path="/queues/team" element={<QueuePage scope="team" />} />
        </Route>
        <Route path="*" element={<Navigate to="/loans" replace />} />
      </Route>
    </Routes>
  )
}

function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <BrowserRouter>
          <AppRoutes />
        </BrowserRouter>
      </AuthProvider>
    </QueryClientProvider>
  )
}

export default App
