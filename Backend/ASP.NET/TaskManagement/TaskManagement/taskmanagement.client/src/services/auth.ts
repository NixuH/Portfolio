type AuthResponse = {
    access_token?: string;
    refresh_token?: string;
    expires_in?: number;
    token_type?: string;
};

export async function loginUser(email: string, password: string) {
    const response = await fetch("/api/auth/login", {
        method: "POST",
        headers: {
            "Content-Type": "application/json",
        },
        body: JSON.stringify({ email, password }),
    });

    const result: AuthResponse = await response.json();

    if (!response.ok) {
        throw new Error("Login failed");
    }

    if (result.access_token) {
        sessionStorage.setItem("access_token", result.access_token);
    }

    return result;
}

export async function registerUser(email: string, password: string) {
    const response = await fetch("/api/auth/register", {
        method: "POST",
        headers: {
            "Content-Type": "application/json",
        },
        body: JSON.stringify({ email, password }),
    });
    const result: AuthResponse = await response.json();

    if (!response.ok) {
        throw new Error("Registration failed");
    }

    return result;
}

export function logoutUser() {
    sessionStorage.removeItem("access_token");
}