export const MINIMUM_VISUAL_LOADING_MS = 1200;

export async function withMinimumVisualDelay<T>(
  task: Promise<T>,
  minimumMs = MINIMUM_VISUAL_LOADING_MS,
): Promise<T> {
  const [result] = await Promise.all([
    task,
    new Promise<void>((resolve) => setTimeout(resolve, minimumMs)),
  ]);

  return result;
}
