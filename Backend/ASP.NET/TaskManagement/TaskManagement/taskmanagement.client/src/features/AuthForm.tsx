import { useState } from "react";
import { useForm } from "react-hook-form";
import { useNavigate } from "react-router-dom";
import { loginUser, registerUser } from "../services/auth";

type AuthFormData = {
    email: string;
    password: string;
    confirmPassword?: string;
};

type AuthFormType = "login" | "register";

type AuthFormProps = {
    type: AuthFormType;
};

export default function AuthForm({ type }: AuthFormProps) {
    const {
        register,
        handleSubmit,
        reset,
        formState: { errors },
    } = useForm<AuthFormData>();

    const [error, setError] = useState<string | null>(null);

    const navigate = useNavigate();

    const onSubmit = async (data: AuthFormData) => {
        setError(null);

        try {
            if (type === "login") {
                await loginUser(data.email, data.password);
            } else {
                await registerUser(data.email, data.password);
            }

            reset();

            if (type === "login")
                navigate("/");
            else 
                navigate("/login")

        } catch (error) {
            console.error(error);

            setError(
                type === "login"
                    ? "Invalid email or password."
                    : "Failed to create an account."
            );
        }
    };

    return (
        <form onSubmit={handleSubmit(onSubmit)}>
            {error && <p>{error}</p>}

            <input
                type="email"
                placeholder="Email"
                {...register("email", {
                    required: "Email is required.",
                    maxLength: {
                        value: 254,
                        message: "Email must be at most 254 characters long.",
                    },
                })}
            />
            <br />

            {errors.email && <p>{errors.email.message}</p>}

            <input
                type="password"
                placeholder="Password"
                {...register("password", {
                    required: "Password is required.",
                    minLength: {
                        value: 8,
                        message: "Password must be at least 8 characters long.",
                    },
                    maxLength: {
                        value: 128,
                        message: "Password must be at most 128 characters long.",
                    },
                    validate: {
                        uppercase: value =>
                            /[A-Z]/.test(value) ||
                            "Password must contain an uppercase letter.",
                        lowercase: value =>
                            /[a-z]/.test(value) ||
                            "Password must contain an lowercase letter.",
                        digit: value =>
                            /\d/.test(value) ||
                            "Password must contain a digit.",
                    },
                })}
            />
            <br />

            {errors.password && <p>{errors.password.message}</p>}

            {type === "register" && (
                <>
                    <input
                        type="password"
                        placeholder="Confirm password"
                        {...register("confirmPassword", {
                            required: "Password confirmation is required.",
                            validate: (value, formValues) =>
                                value === formValues.password ||
                                "Passwords do not match.",
                        })}
                    />
                    <br />

                    {errors.confirmPassword && (
                        <p>{errors.confirmPassword.message}</p>
                    )}
                </>
            )}
            <br />
            <button type="submit">
                {type === "login" ? "Log in" : "Register"}
            </button>
        </form>
    );
}