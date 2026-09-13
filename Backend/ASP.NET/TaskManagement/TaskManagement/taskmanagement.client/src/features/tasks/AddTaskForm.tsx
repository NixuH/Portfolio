import { useState } from "react";
import { useForm } from "react-hook-form";
import type { Task } from "./types";

type TaskFormData = {
    title: string;
    description: string;
};

type AddTaskFormProps = {
    onTaskCreated: (task: Task) => void;
};

export default function AddTaskForm({ onTaskCreated }: AddTaskFormProps) {
    const [isOpen, setIsOpen] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const {
        register,
        handleSubmit,
        reset,
        formState: { errors },
    } = useForm<TaskFormData>();

    const onSubmit = async (data: TaskFormData) => {
        setError(null);

        const token = sessionStorage.getItem("access_token");

        if (!token) {
            return;
        }

        try {
            const response = await fetch("/api/tasks", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                    "Authorization": `Bearer ${token}`,
                },
                body: JSON.stringify(data),
            });

            if (!response.ok) {
                console.error("Failed to create task:", response.status);
                setError("Failed to create task.");
                return;
            }

            const task: Task = await response.json();

            onTaskCreated(task);

            reset();
            setIsOpen(false);
        } catch (error) {
            console.error("Request failed:", error);
            setError("An error occurred while creating the task.");
        }
    };

    return (
        <section>
            <button onClick={() => setIsOpen(prev => !prev)}>
                {isOpen
                    ? "Hide task creator"
                    : "Add task"}
            </button>

            {isOpen && (
                <form onSubmit={handleSubmit(onSubmit)}>
                    {error && <p>{error}</p>}

                    <div>
                        <input
                            type="text"
                            placeholder="Title"
                            {...register("title", {
                                required: "Title is required.",
                                maxLength: {
                                    value: 100,
                                    message:
                                        "Title must be at most 100 characters.",
                                },
                            })}
                        />

                        {errors.title && (
                            <p>{errors.title.message}</p>
                        )}
                    </div>

                    <div>
                        <textarea
                            placeholder="Description"
                            {...register("description", {
                                maxLength: {
                                    value: 1000,
                                    message:
                                        "Description must be at most 1000 characters.",
                                },
                            })}
                        />

                        {errors.description && (
                            <p>{errors.description.message}</p>
                        )}
                    </div>

                    <div>
                        <button type="submit">
                            Add
                        </button>
                    </div>
                </form>
            )}
        </section>
    );
}
