import { Interval, IntervalKey, IntervalRsiCandle } from "@/types";
import { mapIntervalToLabel } from "@/utils";
import { TooltipArrow } from "@radix-ui/react-tooltip";
import classNames from "classnames";
import { Toggle } from "./ui/toggle";
import { Tooltip, TooltipContent, TooltipTrigger } from "./ui/tooltip";

function renderRsiCandleValue(value?: number) {
  if (!value)
    return (
      <td className="px-1 text-center text-xs">
        <span className="block min-w-8">-</span>
      </td>
    );

  // TODO: Add darker colors for 20-30 and 70-80
  // TODO: Add indicator when close is equal to high or low (borders?)
  return (
    <td
      className={classNames("px-1 text-xs", {
        "bg-red-800": value <= 10,
        "bg-red-600": value <= 20,
        "bg-red-400": value <= 30,
        "bg-green-400": value >= 70,
        "bg-green-600": value >= 80,
        "bg-green-800": value >= 90,
        "text-red-600": value < 50,
        "text-green-600": value > 50,
        "!text-white": value <= 30 || value >= 70,
      })}
    >
      <span className="block min-w-8">{value.toFixed(2)}</span>
    </td>
  );
}

interface ISymbolRsiCandlesCardProps {
  symbol: string;
  intervalRsiCandles: IntervalRsiCandle[];
  onIntervalToggle: (interval: IntervalKey, enabled: boolean) => void;
}

function SymbolRsiCandlesCard({
  symbol,
  intervalRsiCandles,
  onIntervalToggle,
}: ISymbolRsiCandlesCardProps) {
  const sortedintervalRsiCandles = intervalRsiCandles.sort(
    (a, b) => Interval[a.interval] - Interval[b.interval],
  );

  return (
    <div className="flex flex-col rounded border-2 border-neutral-300 px-3 py-2">
      <span className="font-semibold uppercase">{symbol}</span>
      <hr className="border-1 mb-2 border-neutral-300" />
      <table>
        <tbody>
          {sortedintervalRsiCandles.map(({ interval, candle }) => (
            <Tooltip key={interval} delayDuration={0}>
              <TooltipTrigger asChild>
                <tr key={interval}>
                  <td className="pr-2">
                    {/* TODO: Add the interval toggling logic later */}
                    <Toggle
                      size="xs"
                      //   TODO: Use pressed and load state from localstorage
                      onPressedChange={(enabled) =>
                        onIntervalToggle(interval, enabled)
                      }
                    >
                      <span>{mapIntervalToLabel(interval)}</span>
                    </Toggle>
                  </td>
                  {renderRsiCandleValue(candle?.low)}
                  {renderRsiCandleValue(candle?.close)}
                  {renderRsiCandleValue(candle?.high)}
                </tr>
              </TooltipTrigger>
              <TooltipContent className="bg-neutral-800 text-white">
                <span>
                  {candle?.time
                    ? new Date(candle.time * 1000).toLocaleString()
                    : "No candle"}
                </span>
                <TooltipArrow />
              </TooltipContent>
            </Tooltip>
          ))}
        </tbody>
      </table>
    </div>
  );
}

export default SymbolRsiCandlesCard;
