/**
 * Registered-device key (Online/Offline licensing). A restaurant till/terminal gets it once by
 * entering the platform admin's one-time code; it is then sent with every API request.
 */
const DEVICE_KEY = "fooderaDeviceKey";
const DEVICE_NAME = "fooderaDeviceName";

export function getDeviceKey(): string | null {
  if (typeof window === "undefined") return null;
  try {
    return localStorage.getItem(DEVICE_KEY);
  } catch {
    return null;
  }
}

export function getDeviceName(): string | null {
  if (typeof window === "undefined") return null;
  try {
    return localStorage.getItem(DEVICE_NAME);
  } catch {
    return null;
  }
}

export function storeDevice(key: string, name: string): void {
  localStorage.setItem(DEVICE_KEY, key);
  localStorage.setItem(DEVICE_NAME, name);
}

export function clearDevice(): void {
  try {
    localStorage.removeItem(DEVICE_KEY);
    localStorage.removeItem(DEVICE_NAME);
  } catch {
    // ignore
  }
}
