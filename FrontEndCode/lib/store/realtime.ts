import { HubConnectionBuilder, LogLevel, type HubConnection } from "@microsoft/signalr";
import { api } from "@/lib/api";
import { getStoredToken } from "@/lib/auth-client";

export type StoreRealtimeEvent = { title: string; message: string; type: string };

/**
 * Opens the store's live channel and calls back for every alert the server pushes. Returns a function that closes it.
 * Without a connection the screens simply keep polling, so a failure here is never shown to the user.
 */
export function connectStoreRealtime(onAlert: (event: StoreRealtimeEvent) => void): () => void {
  const base = (api.defaults.baseURL ?? "").replace(/\/api\/?$/i, "");
  const connection: HubConnection = new HubConnectionBuilder()
    .withUrl(`${base}/hubs/store`, { accessTokenFactory: () => getStoredToken() ?? "", withCredentials: false })
    .withAutomaticReconnect()
    .configureLogging(LogLevel.None)
    .build();

  connection.on("storeAlert", (event: StoreRealtimeEvent) => onAlert(event));
  void connection.start().catch(() => undefined);

  return () => {
    void connection.stop().catch(() => undefined);
  };
}
