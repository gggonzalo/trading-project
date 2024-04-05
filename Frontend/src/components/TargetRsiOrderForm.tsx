import { mapIntervalToLabel } from "@/utils";
import { zodResolver } from "@hookform/resolvers/zod";
import Decimal from "decimal.js";
import { useEffect } from "react";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { Interval, IntervalKey, OrderSide, SymbolDetails } from "../types";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
} from "./ui/alert-dialog";
import { Button } from "./ui/button";
import {
  Form,
  FormControl,
  FormDescription,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from "./ui/form";
import { Input } from "./ui/input";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "./ui/select";

function createFormSchema(symbolDetails: SymbolDetails) {
  const { priceIncrement } = symbolDetails;

  return z.object({
    side: z.nativeEnum(OrderSide),
    interval: z.nativeEnum(Interval),
    quoteQty: z.string().superRefine((val, ctx) => {
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

      if (
        priceIncrement &&
        new Decimal(val).mod(priceIncrement).toNumber() !== 0
      )
        ctx.addIssue({
          code: z.ZodIssueCode.custom,
          message: `Value must be increment of ${symbolDetails.priceIncrement}`,
        });
    }),
    targetRsi: z.coerce.number().min(1).max(100).multipleOf(0.01),
    // #region Price todo
    // quantity: z.string().superRefine((val, ctx) => {
    //   if (!val) {
    //     ctx.addIssue({
    //       code: z.ZodIssueCode.custom,
    //       message: "Value can't be empty",
    //     });

    //     return;
    //   }

    //   if (isNaN(Number(val))) {
    //     ctx.addIssue({
    //       code: z.ZodIssueCode.custom,
    //       message: "Value must be a number",
    //     });

    //     return;
    //   }

    //   if (
    //     quantityIncrement &&
    //     new Decimal(val).mod(quantityIncrement).toNumber() !== 0
    //   )
    //     ctx.addIssue({
    //       code: z.ZodIssueCode.custom,
    //       message: `Value must be an increment of ${symbolDetails.quantityIncrement}`,
    //     });
    // }),
    // #endregion
  });
}

interface TargetRsiOrderFormProps {
  symbolDetails: SymbolDetails;
  onOrderCreated: () => void;
}

export default function TargetRsiOrderForm({
  symbolDetails,
  onOrderCreated,
}: TargetRsiOrderFormProps) {
  const formSchema = createFormSchema(symbolDetails);
  const form = useForm<z.infer<typeof formSchema>>({
    mode: "onChange",
    resolver: zodResolver(formSchema),
    defaultValues: {
      quoteQty: "",
      targetRsi: 0,
    },
  });

  useEffect(() => {
    form.reset();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [symbolDetails]);

  function onSubmit(values: z.infer<typeof formSchema>) {
    const { side, quoteQty, interval, targetRsi } = values;

    fetch("http://localhost:5215/target-rsi-order-instructions", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({
        symbol: symbolDetails.name,
        side,
        quoteQty,
        interval,
        targetRsi,
      }),
    })
      .then(() => {
        onOrderCreated();
      })
      .catch((error) => {
        console.error(error);
        // Handle the error here
      });
  }

  const { quoteAsset, priceIncrement } = symbolDetails;

  return (
    <Form {...form}>
      <form
        id="target-rsi-order-instruction-form"
        onSubmit={form.handleSubmit(onSubmit)}
        className="flex flex-col gap-4"
      >
        <FormField
          control={form.control}
          name="side"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Order side</FormLabel>
              <FormControl>
                <Select
                  onValueChange={field.onChange}
                  defaultValue={field.value}
                >
                  <SelectTrigger>
                    <SelectValue placeholder="Select side" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value={OrderSide.Buy}>
                      {OrderSide.Buy}
                    </SelectItem>
                    <SelectItem value={OrderSide.Sell}>
                      {OrderSide.Sell}
                    </SelectItem>
                  </SelectContent>
                </Select>
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />
        <FormField
          control={form.control}
          name="quoteQty"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Quote quantity</FormLabel>
              <FormControl>
                <Input
                  autoComplete="off"
                  placeholder={`Quantity in ${quoteAsset}`}
                  {...field}
                />
              </FormControl>
              {priceIncrement ? (
                <FormDescription>
                  Quote asset quantity increment is {priceIncrement}
                </FormDescription>
              ) : null}
              <FormMessage />
            </FormItem>
          )}
        />
        <FormField
          control={form.control}
          name="interval"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Interval</FormLabel>
              <FormControl>
                <Select
                  onValueChange={(v) =>
                    field.onChange(Interval[v as IntervalKey])
                  }
                  defaultValue={field.value ? field.value.toString() : ""}
                >
                  <SelectTrigger>
                    <SelectValue placeholder="Select interval" />
                  </SelectTrigger>
                  <SelectContent>
                    {Object.keys(Interval).map((interval) => {
                      return (
                        <SelectItem key={interval} value={interval}>
                          {mapIntervalToLabel(interval as IntervalKey)}
                        </SelectItem>
                      );
                    })}
                  </SelectContent>
                </Select>
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />
        <FormField
          control={form.control}
          name="targetRsi"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Target RSI</FormLabel>
              <FormControl>
                <Input autoComplete="off" type="number" {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />
        {form.formState.isValid ? (
          <AlertDialog>
            <AlertDialogTrigger asChild>
              <Button type="button">Place</Button>
            </AlertDialogTrigger>
            <AlertDialogContent>
              <AlertDialogHeader>
                <AlertDialogTitle>
                  Are you sure you want to place this order?
                </AlertDialogTitle>
                <AlertDialogDescription>
                  This action cannot be undone.
                </AlertDialogDescription>
              </AlertDialogHeader>
              <AlertDialogFooter>
                <AlertDialogCancel>Cancel</AlertDialogCancel>
                <AlertDialogAction
                  type="submit"
                  form="target-rsi-order-instruction-form"
                >
                  Place order
                </AlertDialogAction>
              </AlertDialogFooter>
            </AlertDialogContent>
          </AlertDialog>
        ) : (
          <Button type="button" onClick={() => form.trigger()}>
            Place
          </Button>
        )}
      </form>
    </Form>
  );
}
