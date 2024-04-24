export interface SymbolDetails {
  name: string;
  baseAsset: string;
  quoteAsset: string;
  priceIncrement?: number;
  quantityIncrement?: number;
  orderInstructionsCount: number;
}

// Make sure order is same as in backend
export const Interval = {
  OneMinute: 60,
  FiveMinutes: 300,
  FifteenMinutes: 900,
  OneHour: 3600,
  FourHour: 14400,
  OneDay: 86400,
  OneWeek: 604800,
  OneMonth: 2592000,
} as const;

export type IntervalKey = keyof typeof Interval;
export type IntervalValue = (typeof Interval)[keyof typeof Interval];

export interface Candle {
  time: number;
  open: number;
  high: number;
  low: number;
  close: number;
}

export interface RsiCandle {
  time: number;
  high?: number;
  low?: number;
  close?: number;
}

export enum OrderSide {
  Buy = "Buy",
  Sell = "Sell",
}
export interface TargetRsiOrderInstruction {
  id: string;
  symbol: string;
  side: OrderSide;
  quoteQty: number;
  interval: keyof typeof Interval;
  targetRsi: number;
}

export interface IntervalRsiCandle {
  interval: IntervalKey;
  candle?: RsiCandle;
}

export interface SymbolIntervalRsiCandles {
  symbol: string;
  intervalRsiCandles: IntervalRsiCandle[];
}
