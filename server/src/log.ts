import {
  appendFileSync
} from 'fs';
import {
  join
} from 'path';
import {
  isDebug
} from './env.js';
import {
  formatWithOptions,
  format as utilFormat
} from 'util';

namespace Logger {
  export const enum Colors {
    Default = '\x1b[0;0m',
    Black = '\x1b[0;30m',
    DarkRed = '\x1b[0;31m',
    DarkGreen = '\x1b[0;32m',
    DarkYellow = '\x1b[0;33m',
    DarkBlue = '\x1b[0;34m',
    DarkMagenta = '\x1b[0;35m',
    DarkCyan = '\x1b[0;36m',
    Gray = '\x1b[0;37m',
    DarkGray = '\x1b[1;90m',
    Red = '\x1b[1;91m',
    Green = '\x1b[1;92m',
    Yellow = '\x1b[1;93m',
    Blue = '\x1b[1;94m',
    Magenta = '\x1b[1;95m',
    Cyan = '\x1b[1;96m',
    White = '\x1b[1;97m',
  }
  type LogArgs = [format: string, ...args: any[]];
  const enum LogType {
    Debug = 'Debug',
    Error = 'Error',
    Standard = 'Log'
  }
  const LogFile = join(process.cwd(), 'server', 'logs', 'log.log');
  export const colorString = (color: Colors, str: string, useColors: boolean = true) => {
    if (!useColors) return str;
    return `${color}${str}\x1b[0m`;
  }
  const logTypeToColor = (type: LogType) => {
    switch (type) {
      case LogType.Debug: return Colors.Gray;
      case LogType.Error: return Colors.Red;
      case LogType.Standard: return Colors.Yellow;
    }
  }
  const constructFormatString = (type: LogType, format: string, colors: boolean = true) => {
    return `${colorString(Colors.Green, `${new Date().toISOString()}`, colors)} ${colorString(logTypeToColor(type), `[${type}]`, colors)}: ${format}`;
  }
  const internalLog = (type: LogType, format: string, ...args: any[]) => {
    console.log(formatWithOptions({
      colors: true,
      showProxy: false,
    }, constructFormatString(type, format), ...args));
    if (isDebug) return;
    const str = formatWithOptions({
      showHidden: false,
      compact: true,
      colors: false,
      showProxy: false,
    }, constructFormatString(type, format, false));
    appendFileSync(LogFile, str);
  }

  export function error(...args: LogArgs | [error: Error]) {
    internalLog(LogType.Error, args[0] instanceof Error ? '%O' : args[0], ...args.slice(+!(args[0] instanceof Error)));
  }
  export function log(...args: LogArgs) { internalLog(LogType.Standard, ...args) }
  export function debugLog(...args: LogArgs) { if (!isDebug) return; internalLog(LogType.Debug, ...args); }
}

export default Logger;
