export interface SymbolDetails {
  name: string;
  baseAsset: string;
  quoteAsset: string;
  priceIncrement?: number;
  quantityIncrement?: number;
  orderInstructionsCount: number;
}

export interface PriceFormat {
  minMove: number;
  precision: number;
}

export interface QuantityFormat {
  minMove: number;
  precision: number;
}

export interface SymbolInfo {
  symbol: string;
  baseAsset: string;
  quoteAsset: string;
  priceFormat: PriceFormat;
  quantityFormat: QuantityFormat;
}

export type Interval =
  | "OneMinute"
  | "FiveMinutes"
  | "FifteenMinutes"
  | "OneHour"
  | "FourHour"
  | "OneDay"
  | "OneWeek"
  | "OneMonth";

// TODO: Remove these types below. If need seconds, create util functions to convert to seconds.
export const IntervalObj = {
  OneMinute: 60,
  FiveMinutes: 300,
  FifteenMinutes: 900,
  OneHour: 3600,
  FourHour: 14400,
  OneDay: 86400,
  OneWeek: 604800,
  OneMonth: 2592000,
} as const;

export type IntervalKey = keyof typeof IntervalObj;
export type IntervalValue = (typeof IntervalObj)[keyof typeof IntervalObj];

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

export interface Alert {
  id: string;
  symbol: string;
  valueOnCreation: number;
  valueTarget: number;
  createdAt: string;
}

export interface TargetRsiOrderInstruction {
  id: string;
  symbol: string;
  side: OrderSide;
  quoteQty: number;
  interval: keyof typeof IntervalObj;
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
