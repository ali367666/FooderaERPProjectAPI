import { api } from "@/lib/api";
import { toApiFormError } from "@/lib/api-error";

export type ReservationStatus =
  | "Pending" | "Confirmed" | "Seated" | "Completed" | "Cancelled" | "NoShow";

export type ReservationDto = {
  id: number;
  restaurantId: number;
  restaurantName: string;
  tableId: number | null;
  tableName: string | null;
  guestName: string;
  guestPhone: string;
  guestEmail: string | null;
  guestCount: number;
  reservationDate: string;   // ISO date
  reservationTime: string;   // "HH:mm"
  durationMinutes: number;
  status: ReservationStatus;
  note: string | null;
  confirmedAt: string | null;
  createdAtUtc: string;
};

export type CreateReservationInput = {
  restaurantId: number;
  tableId?: number | null;
  guestName: string;
  guestPhone: string;
  guestEmail?: string | null;
  guestCount: number;
  reservationDate: string;
  reservationTime: string;
  durationMinutes?: number;
  note?: string | null;
};

export async function getReservations(date?: string): Promise<ReservationDto[]> {
  try {
    const params = date ? { date } : {};
    const res = await api.get<ReservationDto[]>("/Reservations", { params });
    return Array.isArray(res.data) ? res.data : [];
  } catch (e) {
    throw toApiFormError(e, "Rezervasiyalar yüklənmədi");
  }
}

export async function createReservation(data: CreateReservationInput): Promise<ReservationDto> {
  try {
    const res = await api.post<ReservationDto>("/Reservations", data);
    return res.data;
  } catch (e) {
    throw toApiFormError(e, "Rezervasiya yaradılmadı");
  }
}

export async function updateReservation(id: number, data: Partial<CreateReservationInput>): Promise<ReservationDto> {
  try {
    const res = await api.put<ReservationDto>(`/Reservations/${id}`, data);
    return res.data;
  } catch (e) {
    throw toApiFormError(e, "Rezervasiya yenilənmədi");
  }
}

export async function changeReservationStatus(
  id: number,
  action: "confirm" | "seat" | "complete" | "cancel" | "noshow",
): Promise<ReservationDto> {
  try {
    const res = await api.post<ReservationDto>(`/Reservations/${id}/status`, null, {
      params: { action },
    });
    return res.data;
  } catch (e) {
    throw toApiFormError(e, "Status dəyişdirilmədi");
  }
}

export async function deleteReservation(id: number): Promise<void> {
  try {
    await api.delete(`/Reservations/${id}`);
  } catch (e) {
    throw toApiFormError(e, "Rezervasiya silinmədi");
  }
}
