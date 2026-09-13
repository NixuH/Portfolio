import { useEffect, useState } from "react";
import TasksList from "../features/tasks/TasksList";
import AddTaskForm from "../features/tasks/AddTaskForm";
import type { Task } from "../features/tasks/types";

export default function Dashboard() {
    const [tasks, setTasks] = useState<Task[]>([]);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        const fetchTasks = async () => {
            try {
                const token = sessionStorage.getItem("access_token");

                if (!token) {
                    setError("Log in to view your tasks.");
                    return;
                }

                const response = await fetch("/api/tasks", {
                    headers: {
                        Authorization: `Bearer ${token}`,
                    },
                });

                if (!response.ok) {
                    setError("Failed to load tasks.");
                    return;
                }

                const data = await response.json();
                setTasks(data);
            } catch (error) {
                console.error("Request failed:", error);
                setError("An error occurred while loading tasks.");
            }
        };

        fetchTasks();
    }, []);

    return (
        <main>
            <h1>Dashboard</h1>
            {error && <p>{error}</p>}

            {!error && (
                <>
                    <AddTaskForm
                        onTaskCreated={(task) => {
                            setTasks(prev => [...prev, task]);
                        }}
                    />

                    <TasksList
                        tasks={tasks}
                        onTaskUpdated={(updatedTask) => {
                            setTasks(prev =>
                                prev.map(task =>
                                    task.id === updatedTask.id
                                        ? updatedTask
                                        : task
                                )
                            );
                        }}
                        onTaskRemoved={(taskId) => {
                            setTasks(prev =>
                                prev.filter(task => task.id !== taskId)
                            );
                        }}
                    />
                </>

            )}
        </main>
    );
}