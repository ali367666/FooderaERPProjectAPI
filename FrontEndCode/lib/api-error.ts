import axios from "axios";

export type FieldErrors = Record<string, string[]>;

export class ApiFormError extends Error {
  fieldErrors: FieldErrors;

  constructor(message: string, fieldErrors: FieldErrors = {}) {
    super(message);
    this.name = "ApiFormError";
    this.fieldErrors = fieldErrors;
  }
}

function toFieldErrors(value: unknown): FieldErrors {
  if (!value || typeof value !== "object") return {};
  const source = value as Record<string, unknown>;
  const result: FieldErrors = {};

  for (const [key, raw] of Object.entries(source)) {
    if (!key) continue;
    const normalizedKey = key.toLowerCase();
    if (Array.isArray(raw)) {
      const messages = raw.filter((item): item is string => typeof item === "string");
      if (messages.length > 0) result[normalizedKey] = messages;
      continue;
    }
    if (typeof raw === "string" && raw.trim()) {
      result[normalizedKey] = [raw];
    }
  }

  return result;
}

function joinFieldErrors(fieldErrors: FieldErrors): string {
  const messages = Object.values(fieldErrors).flat().filter(Boolean);
  return messages.join(" ");
}

export function getFieldErrorMessage(
  fieldErrors: FieldErrors,
  ...keys: string[]
): string | undefined {
  for (const key of keys) {
    const entry = fieldErrors[key.toLowerCase()];
    if (entry && entry.length > 0) return entry[0];
  }
  return undefined;
}

export function toApiFormError(error: unknown, fallback: string): ApiFormError {
  if (error instanceof ApiFormError) return error;

  if (axios.isAxiosError(error)) {
    const status = error.response?.status;

    const data = error.response?.data as
      | {
          message?: string;
          Message?: string;
          title?: string;
          errors?: unknown;
          Errors?: unknown;
        }
      | undefined;

    if (status === 401) {
      return new ApiFormError(
        "Your session has expired. Please sign in again.",
        {},
      );
    }

    if (status === 403) {
      const bodyMsg = (data?.message || data?.Message || "").trim();
      const useful =
        bodyMsg &&
        !/^forbidden\.?$/i.test(bodyMsg) &&
        !/^request failed with status code 403$/i.test(bodyMsg);
      return new ApiFormError(
        useful
          ? bodyMsg
          : "You do not have permission to perform this action.",
        {},
      );
    }

    const fieldErrors = toFieldErrors(data?.errors ?? data?.Errors);
    const primaryMessage =
      data?.message || data?.Message || data?.title || error.message || fallback;

    if (
      primaryMessage.trim().toLowerCase() === "validation failed" &&
      Object.keys(fieldErrors).length > 0
    ) {
      return new ApiFormError(joinFieldErrors(fieldErrors), fieldErrors);
    }

    if (Object.keys(fieldErrors).length > 0) {
      const combined = joinFieldErrors(fieldErrors);
      return new ApiFormError(combined || primaryMessage, fieldErrors);
    }

    return new ApiFormError(primaryMessage || fallback, {});
  }

  if (error instanceof Error) {
    return new ApiFormError(error.message || fallback, {});
  }

  return new ApiFormError(fallback, {});
}
