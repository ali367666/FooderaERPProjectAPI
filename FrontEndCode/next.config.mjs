import { readFileSync } from "node:fs"

const { version } = JSON.parse(readFileSync(new URL("./package.json", import.meta.url), "utf8"))

/** @type {import('next').NextConfig} */
const nextConfig = {
  typescript: {
    ignoreBuildErrors: true,
  },
  images: {
    unoptimized: true,
  },
  // Shown as "Versiya" on the POS login screen.
  env: {
    NEXT_PUBLIC_APP_VERSION: version,
  },
  // Lets the dev server (and its HMR websocket) be reached from a phone on the
  // LAN via the PC's IP — Next.js otherwise blocks cross-origin dev requests.
  allowedDevOrigins: ["192.168.0.2"],
}

export default nextConfig
