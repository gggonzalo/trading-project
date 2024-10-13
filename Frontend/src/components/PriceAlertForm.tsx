import CandlesStreamingService from "@/services/CandlesStreamingService";
import useAppStore from "@/useAppStore";
import { zodResolver } from "@hookform/resolvers/zod";
import { ArrowLeft, TrendingDown, TrendingUp } from "lucide-react";
import { useEffect, useState } from "react";
import { useForm, useWatch } from "react-hook-form";
import OneSignal from "react-onesignal";
import { z } from "zod";
import { Button } from "./ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from "./ui/card";
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from "./ui/form";
import { Input } from "./ui/input";
import { toast } from "./ui/use-toast";

const formSchema = z.object({
  price: z.string().superRefine((val, ctx) => {
    if (!val) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        message: "Value can't be empty",
      });

      return;
    }

    if (isNaN(Number(val))) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        message: "Value must be a number",
      });

      return;
    }
  }),
});

type FormValues = z.infer<typeof formSchema>;

type PriceAlertFormProps = {
  onAlertCreated: () => void;
};

function PriceAlertForm({ onAlertCreated }: PriceAlertFormProps) {
  // Store
  const symbolInfo = useAppStore((state) => state.symbolInfo);
  const interval = useAppStore((state) => state.interval);

  // State
  const [currentPrice, setCurrentPrice] = useState<number | null>(null);

  // Form
  const form = useForm<FormValues>({
    defaultValues: {
      price: "",
    },
    mode: "onChange",
    resolver: zodResolver(formSchema),
  });

  const price = useWatch({ control: form.control, name: "price" });

  const onSubmit = (data: FormValues) => {
    const { price: valueTarget } = data;

    fetch("http://localhost:5215/alerts", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({
        symbol: symbolInfo,
        valueTarget: Number(valueTarget),
        subscriptionId: OneSignal.User.PushSubscription.id,
      }),
    })
      .then(() => {
        onAlertCreated();

        toast({
          title: "Price alert created",
          description:
            "You will receive a notification when the price hits the target.",
        });

        form.reset();
      })
      .catch((error) => {
        console.error(error);
        // TODO: Handle the error
      });
  };

  // Effects
  useEffect(() => {
    if (!symbolInfo?.symbol) return;

    const candleUpdatesSubscription = CandlesStreamingService.subscribe(
      symbolInfo.symbol,
      interval,
      (candle) => setCurrentPrice(candle.close),
    );

    return () => {
      candleUpdatesSubscription.unsubscribe();

      setCurrentPrice(null);
    };
  }, [interval, symbolInfo?.symbol]);

  const renderHelperText = () => {
    if (!symbolInfo?.symbol) return;

    const priceAsNumber = Number(price);

    if (!priceAsNumber || !currentPrice) return;

    const isAlertBullish = priceAsNumber > currentPrice;

    return (
      <div className="mt-6 flex flex-col gap-3">
        <p className="text-center text-xs text-muted-foreground">
          Creating {isAlertBullish ? "bullish" : "bearish"} alert for{" "}
          {symbolInfo.symbol}:
        </p>
        <div className="grid grid-cols-3">
          <span className="justify-self-end text-sm font-semibold">
            {currentPrice}
          </span>
          {isAlertBullish ? (
            <TrendingUp className="size-6 shrink-0 justify-self-center stroke-[#26a69a]" />
          ) : (
            <TrendingDown className="size-6 shrink-0 justify-self-center stroke-[#ef5350]" />
          )}
          <span className="justify-self-start text-sm font-semibold">
            {priceAsNumber}
          </span>
        </div>
      </div>
    );
  };
  return (
    <Card>
      <CardHeader>
        <CardTitle>Price alert</CardTitle>
        <CardDescription>Create your price alert</CardDescription>
      </CardHeader>
      <Form {...form}>
        <form onSubmit={form.handleSubmit(onSubmit)}>
          <CardContent>
            <FormField
              control={form.control}
              name="price"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Target price</FormLabel>
                  <FormControl>
                    <Input {...field} autoComplete="off" autoFocus />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            {renderHelperText()}
          </CardContent>
          <CardFooter className="flex justify-between gap-4">
            <Button
              type="button"
              variant="secondary"
              onClick={() =>
                useAppStore.setState({ activeUserPanel: "AlertsButtons" })
              }
            >
              <ArrowLeft className="size-4" />
            </Button>
            <Button type="submit" disabled={!symbolInfo}>
              Create alert
            </Button>
          </CardFooter>
        </form>
      </Form>
    </Card>
  );
}

export default PriceAlertForm;
