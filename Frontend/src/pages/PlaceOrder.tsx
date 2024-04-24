import CandleStickChart from "@/components/CandleStickChart";
import TargetRsiOrderForm from "@/components/TargetRsiOrderForm";
import { Button } from "@/components/ui/button";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  Table,
  TableBody,
  TableCell,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { useToast } from "@/components/ui/use-toast";
import RsiCandlesService from "@/services/RsiCandlesService";
import {
  Interval,
  IntervalKey,
  SymbolDetails,
  TargetRsiOrderInstruction,
} from "@/types";
import {
  getCrosshairDataPoint,
  mapIntervalToLabel,
  syncCrosshair,
} from "@/utils";
import { HubConnectionBuilder } from "@microsoft/signalr";
import Decimal from "decimal.js";
import {
  CandlestickData,
  IChartApi,
  ISeriesApi,
  Time,
} from "lightweight-charts";
import { Trash } from "lucide-react";
import { useCallback, useEffect, useRef, useState } from "react";

function PlaceOrder() {
  const dataChartApi = useRef<IChartApi | null>(null);
  const dataSeriesApi = useRef<ISeriesApi<"Candlestick"> | null>(null);
  const rsiChartApi = useRef<IChartApi | null>(null);
  const rsiSeriesApi = useRef<ISeriesApi<"Candlestick"> | null>(null);

  const [symbolsDetails, setSymbolsDetails] = useState<SymbolDetails[]>([]);

  const [selectedSymbolDetails, setSelectedSymbolDetails] =
    useState<SymbolDetails | null>(null);
  const [selectedInterval, setSelectedInterval] = useState<IntervalKey | null>(
    null,
  );

  const [dataChartCandleSticks, setDataChartCandleSticks] = useState<
    CandlestickData[]
  >([]);

  const [targetRsiOrderInstructions, setTargetRsiOrderInstructions] = useState<
    TargetRsiOrderInstruction[]
  >([]);

  const { toast } = useToast();

  const handleDataChartApisReady = useCallback(
    (chartApi: IChartApi, seriesApi: ISeriesApi<"Candlestick">) => {
      dataChartApi.current = chartApi;
      dataSeriesApi.current = seriesApi;

      dataChartApi.current.subscribeClick(({ point }) => {
        if (!point?.y) return;

        console.log(dataSeriesApi.current?.coordinateToPrice(point.y));
      });
    },
    [],
  );
  const handleRsiChartApisReady = useCallback(
    (chartApi: IChartApi, seriesApi: ISeriesApi<"Candlestick">) => {
      rsiChartApi.current = chartApi;
      rsiSeriesApi.current = seriesApi;

      rsiSeriesApi.current.createPriceLine({
        price: 70,
        color: "#000",
        lineWidth: 2,
        lineStyle: 1,
        axisLabelVisible: true,
      });
      rsiSeriesApi.current.createPriceLine({
        price: 30,
        color: "#000",
        lineWidth: 2,
        lineStyle: 1,
        axisLabelVisible: true,
      });

      rsiChartApi.current.subscribeClick(({ point }) => {
        if (!point?.y) return;

        console.log(seriesApi.coordinateToPrice(point.y));
      });
    },
    [],
  );

  const fetchTargetRsiOrderInstructions = (symbol: string) => {
    fetch(
      `http://localhost:5215/target-rsi-order-instructions?symbol=${symbol}`,
    )
      .then((response) => response.json())
      .then((data) => {
        setTargetRsiOrderInstructions(data);
      });
  };

  const handleOnTargetRsiOrderCreated = () => {
    fetchTargetRsiOrderInstructions(selectedSymbolDetails!.name);
  };

  const handleDeleteInstruction = (instructionId: string) => {
    fetch(
      `http://localhost:5215/target-rsi-order-instructions/${instructionId}?symbol=${selectedSymbolDetails!.name}`,
      {
        method: "DELETE",
      },
    ).then(() => {
      setTargetRsiOrderInstructions(
        targetRsiOrderInstructions.filter((i) => i.id !== instructionId),
      );
    });
  };

  useEffect(() => {
    if (selectedSymbolDetails === null) return;

    fetchTargetRsiOrderInstructions(selectedSymbolDetails.name);
  }, [selectedSymbolDetails]);

  useEffect(() => {
    fetch("http://localhost:5215/symbols")
      .then((response) => response.json())
      .then((data) => {
        const symbolsDetails = data;

        setSymbolsDetails(symbolsDetails);
      });
  }, []);

  useEffect(() => {
    if (selectedSymbolDetails === null || selectedInterval === null) return;

    // todo: add startTime and endTime to the query when lazy loading is implemented
    fetch(
      `http://localhost:5215/candles?symbol=${selectedSymbolDetails.name}&interval=${selectedInterval}`,
    )
      .then((response) => response.json())
      .then((data: any) => {
        dataSeriesApi.current?.setData(data);

        setDataChartCandleSticks(data);
      });
  }, [selectedSymbolDetails, selectedInterval]);

  // TODO: Consolidate useEffects with same dependencies?
  useEffect(() => {
    if (!selectedSymbolDetails || !selectedInterval) return;

    const connection = new HubConnectionBuilder()
      .withUrl("http://localhost:5215/candles-hub")
      .build();

    connection.on("CandleUpdate", ({ candle }: any) => {
      dataSeriesApi.current?.update(candle);
    });

    connection.start().then(() => {
      connection
        .invoke(
          "SubscribeToCandleUpdates",
          [selectedSymbolDetails.name],
          [selectedInterval],
        )
        .catch((e) => {
          toast({
            title: "Error subscribing to candle updates.",
            description: e.message,
            variant: "destructive",
          });
        });
    });

    return () => {
      connection.stop();
    };
  }, [selectedSymbolDetails, selectedInterval, toast]);

  useEffect(() => {
    if (!selectedSymbolDetails?.priceIncrement || !selectedInterval) return;

    const symbolDetailsPrecision = new Decimal(
      selectedSymbolDetails.priceIncrement,
    ).decimalPlaces();

    // TODO: Fix bug api not available on first symbol select
    dataSeriesApi.current?.applyOptions({
      priceFormat: {
        type: "price",
        minMove: selectedSymbolDetails.priceIncrement,
        precision: symbolDetailsPrecision,
      },
    });

    // TODO: See if we should set the format for the rsi chart or just let it infer it. We could pass an initial prop to the chart component
    if (
      selectedInterval === "OneMinute" ||
      selectedInterval === "FiveMinutes" ||
      selectedInterval === "FifteenMinutes" ||
      selectedInterval === "OneHour" ||
      selectedInterval === "FourHour"
    ) {
      const timeScaleOptions = {
        timeScale: {
          timeVisible: true,
        },
      };

      dataChartApi.current?.applyOptions(timeScaleOptions);
      rsiChartApi.current?.applyOptions(timeScaleOptions);
    }
  }, [selectedSymbolDetails, selectedInterval]);

  useEffect(() => {
    if (dataChartCandleSticks.length === 0) return;

    const rsiCandlesService = new RsiCandlesService();
    const rsiCandles = rsiCandlesService
      .generateRsiCandles(dataChartCandleSticks as any)
      .map((c) => {
        return {
          ...c,
          // TODO: See why number is not assignable to Time
          time: c.time as Time,
          // TODO: Leave 'close' for now until we implement custom RSI series type
          open: c.close,
        };
      });

    rsiSeriesApi.current?.setData(rsiCandles);

    // TODO: See if there's a solution without using setTimeout
    setTimeout(() => {
      rsiSeriesApi.current?.priceScale().applyOptions({
        minimumWidth: dataSeriesApi.current?.priceScale().width(),
      });
    }, 100);

    dataChartApi.current
      ?.timeScale()
      .subscribeVisibleLogicalRangeChange((timeRange) => {
        if (timeRange)
          rsiChartApi.current?.timeScale().setVisibleLogicalRange(timeRange);
      });

    rsiChartApi.current
      ?.timeScale()
      .subscribeVisibleLogicalRangeChange((timeRange) => {
        if (timeRange)
          dataChartApi.current?.timeScale().setVisibleLogicalRange(timeRange);
      });

    dataChartApi.current?.subscribeCrosshairMove((param) => {
      const dataPoint = getCrosshairDataPoint(dataSeriesApi.current!, param);

      syncCrosshair(rsiChartApi.current!, rsiSeriesApi.current!, dataPoint);
    });
    rsiChartApi.current?.subscribeCrosshairMove((param) => {
      const dataPoint = getCrosshairDataPoint(rsiSeriesApi.current!, param);

      syncCrosshair(dataChartApi.current!, dataSeriesApi.current!, dataPoint);
    });
  }, [dataChartCandleSticks]);

  return (
    <div className="flex flex-col items-center gap-20">
      <div className="flex gap-2">
        {/* TODO: Replace with combobox for search and suggestions */}
        <Select
          onValueChange={(s) =>
            setSelectedSymbolDetails(symbolsDetails.find((d) => d.name === s)!)
          }
          disabled={symbolsDetails.length === 0}
        >
          <SelectTrigger className="w-[175px]">
            <SelectValue placeholder="Select symbol" />
          </SelectTrigger>
          <SelectContent>
            {symbolsDetails.map((d) => (
              <SelectItem key={d.name} value={d.name}>
                {`${d.name} (${d.orderInstructionsCount} instr.)`}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        <Select
          onValueChange={(i) => setSelectedInterval(i as IntervalKey)}
          disabled={symbolsDetails.length === 0}
        >
          <SelectTrigger className="w-[175px]">
            <SelectValue placeholder="Select interval" />
          </SelectTrigger>
          <SelectContent>
            {Object.keys(Interval).map((interval) => (
              <SelectItem key={interval} value={interval}>
                {mapIntervalToLabel(interval as IntervalKey)}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>
      <div className="flex min-w-0 flex-col gap-5">
        {selectedSymbolDetails ? (
          <div className="flex gap-10">
            <div className="w-[600px] overflow-auto">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableCell>Symbol</TableCell>
                    <TableCell>Side</TableCell>
                    <TableCell>QuoteQty</TableCell>
                    <TableCell>Interval</TableCell>
                    <TableCell>TargetRsi</TableCell>
                    <TableCell></TableCell>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {targetRsiOrderInstructions.map((instruction) => (
                    <TableRow key={instruction.id}>
                      <TableCell>{instruction.symbol}</TableCell>
                      <TableCell>{instruction.side}</TableCell>
                      <TableCell>{instruction.quoteQty}</TableCell>
                      <TableCell>{instruction.interval}</TableCell>
                      <TableCell>{instruction.targetRsi}</TableCell>
                      <TableCell>
                        <Button
                          type="button"
                          size="sm"
                          variant="destructive"
                          onClick={() =>
                            handleDeleteInstruction(instruction.id)
                          }
                        >
                          <Trash size={20} />
                        </Button>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>
            <div className="w-[300px]">
              <TargetRsiOrderForm
                symbolDetails={selectedSymbolDetails}
                onOrderCreated={handleOnTargetRsiOrderCreated}
              />
            </div>
          </div>
        ) : null}
        {/* TODO: Add OHCL legends to charts */}
        {selectedSymbolDetails && selectedInterval ? (
          <div className="flex flex-col gap-2">
            <div className="h-[350px]">
              <CandleStickChart onApisReady={handleDataChartApisReady} />
            </div>
            <div className="h-[175px]">
              <CandleStickChart onApisReady={handleRsiChartApisReady} />
            </div>
          </div>
        ) : null}
      </div>
    </div>
  );
}

export default PlaceOrder;
