import { useState } from "react";
import type { Task, TaskStatus } from "./types";

type TasksListProps = {
    tasks: Task[];
    onTaskUpdated: (task: Task) => void;
    onTaskRemoved: (taskId: number) => void;
};

export default function TasksList({
    tasks,
    onTaskUpdated,
    onTaskRemoved
}: TasksListProps) {
    const [error, setError] = useState<string | null>(null);

    const updateStatus = async (taskId: number, status: TaskStatus) => {
        setError(null);

        const token = sessionStorage.getItem("access_token");

        if (!token) {
            return;
        }

        try {
            const response = await fetch(`/api/tasks/${taskId}/status`, {
                method: "PATCH",
                headers: {
                    "Content-Type": "application/json",
                    "Authorization": `Bearer ${token}`,
                },
                body: JSON.stringify({ status }),
            });

            if (!response.ok) {
                console.error("PATCH status:", response.status);
                setError("Failed to update the task.");
                return;
            }
        
            const updatedTask: Task = await response.json();
        
            onTaskUpdated(updatedTask);
        } catch (error) {
            console.error("PATCH request failed:", error);
            setError("An error occurred while updating the task.");
        }
    };

    const removeTask = async (taskId: number) => {
        setError(null);

        const token = sessionStorage.getItem("access_token");

        if (!token) {
            return;
        }

        try {
            const response = await fetch(`/api/tasks/${taskId}`, {
                method: "DELETE",
                headers: {
                    "Authorization": `Bearer ${token}`,
                },
            });

            if (!response.ok) {
                console.error("DELETE status:", response.status);
                setError("Failed to delete the task.");
                return;
            }

            onTaskRemoved(taskId);
        } catch (error) {
            console.error("DELETE request failed:", error);
            setError("An error occurred while deleting the task.");
        }
    };

    return (
        <div>
            {error && <p>{error}</p>}

            {tasks.map(task => (
                <div key={task.id}>
                    <h3>{task.title}</h3>
                    <p>{task.description}</p>

                    <select
                        value={task.status}
                        onChange={e =>
                            updateStatus(
                                task.id,
                                e.target.value as TaskStatus
                            )
                        }
                    >
                        <option value="NotStarted">Not started</option>
                        <option value="InProgress">In progress</option>
                        <option value="Completed">Completed</option>
                    </select>

                    <button onClick={() => removeTask(task.id)}>
                        Remove
                    </button>
                </div>
            ))}
        </div>
    );
}
