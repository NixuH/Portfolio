export type TaskStatus = "NotStarted" | "InProgress" | "Completed";

export type Task = {
    id: number;
    title: string;
    description: string | null;
    status: TaskStatus;
};