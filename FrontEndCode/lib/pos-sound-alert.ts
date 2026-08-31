import type { CompanySettingsBranding } from "@/lib/services/company-settings-service";

/** Plays the company's configured alert tone (Uyarı Milisaniyə/Zəng Sayısı/Aralıq settings). */
export function playPosAlert(branding: CompanySettingsBranding | null): void {
  if (typeof window === "undefined") return;

  const toneMs = branding?.alertMilliseconds ?? 400;
  const ringCount = branding?.alertRingCount ?? 1;
  const ringIntervalSeconds = branding?.alertRingIntervalSeconds ?? 1;

  const AudioCtx =
    window.AudioContext || (window as unknown as { webkitAudioContext?: typeof AudioContext }).webkitAudioContext;
  if (!AudioCtx) return;
  const ctx = new AudioCtx();
  if (ctx.state === "suspended") void ctx.resume();

  const ringGapMs = Math.max(ringIntervalSeconds * 1000, toneMs + 50);

  for (let i = 0; i < Math.max(ringCount, 1); i++) {
    const startAt = ctx.currentTime + (i * ringGapMs) / 1000;
    const oscillator = ctx.createOscillator();
    const gain = ctx.createGain();
    oscillator.type = "sine";
    oscillator.frequency.value = 880;
    gain.gain.setValueAtTime(0.0001, startAt);
    gain.gain.exponentialRampToValueAtTime(0.3, startAt + 0.02);
    gain.gain.exponentialRampToValueAtTime(0.0001, startAt + toneMs / 1000);
    oscillator.connect(gain);
    gain.connect(ctx.destination);
    oscillator.start(startAt);
    oscillator.stop(startAt + toneMs / 1000 + 0.05);
  }

  const totalMs = Math.max(ringCount, 1) * ringGapMs + 200;
  setTimeout(() => void ctx.close(), totalMs);
}
