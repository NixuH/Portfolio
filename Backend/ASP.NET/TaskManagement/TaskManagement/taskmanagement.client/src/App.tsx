import { BrowserRouter, Link, Route, Routes, useNavigate } from 'react-router-dom';
import Dashboard from './pages/Dashboard';
import Login from './pages/Login';
import Register from './pages/Register';
import { logoutUser } from './services/auth';

function Navigation() {
    const navigate = useNavigate();

    const handleLogout = () => {
        logoutUser();
        navigate("/login");
    };

    const token = sessionStorage.getItem("access_token");

    return (
        <nav>
            <div>
                <Link to="/">Dashboard</Link>
            </div>

            <div>
                {!token &&
                    <Link to="/login">Log in</Link>}
            </div>

            <div>
                {!token &&
                    <Link to="/register">Register</Link>}
            </div>

            {token &&
                <button onClick={handleLogout}>
                    Log out
                </button>}
        </nav>
    );
}

function App() {
    return (
        <BrowserRouter>
            <Navigation />

            <Routes>
                <Route path="/" element={<Dashboard />} />
                <Route path="/login" element={<Login />} />
                <Route path="/register" element={<Register />} />
            </Routes>
        </BrowserRouter>
    );
}

export default App;