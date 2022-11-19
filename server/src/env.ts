export default function getEnv(key: string): string | null {
  return key in process.env ? process.env[key]! : null;
}

export const isDebug = !getEnv('__PRODUCTION');
