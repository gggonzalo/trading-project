import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover";
import { cn } from "@/lib/utils";
import { Check, ChevronsUpDown } from "lucide-react";
import React from "react";
import { Button } from "./ui/button";
import {
  Command,
  CommandEmpty,
  CommandGroup,
  CommandInput,
  CommandItem,
  CommandList,
} from "./ui/command";

const symbols = [
  {
    value: "BTCUSDT",
    label: "Bitcoin",
    icon: "https://cryptologos.cc/logos/bitcoin-btc-logo.svg",
  },
  {
    value: "ETHUSDT",
    label: "Ethereum",
    icon: "https://cryptologos.cc/logos/ethereum-eth-logo.svg",
  },
  {
    value: "BNBUSDT",
    label: "Binance Coin",
    icon: "https://cryptologos.cc/logos/bnb-bnb-logo.svg",
  },
  {
    value: "ADAUSDT",
    label: "Cardano",
    icon: "https://cryptologos.cc/logos/cardano-ada-logo.svg",
  },
  {
    value: "DOGEUSDT",
    label: "Dogecoin",
    icon: "https://cryptologos.cc/logos/dogecoin-doge-logo.svg",
  },
  {
    value: "DOTUSDT",
    label: "Polkadot",
    icon: "https://cryptologos.cc/logos/polkadot-new-dot-logo.svg",
  },
  {
    value: "UNIUSDT",
    label: "Uniswap",
    icon: "https://cryptologos.cc/logos/uniswap-uni-logo.svg",
  },
  {
    value: "LINKUSDT",
    label: "Chainlink",
    icon: "https://cryptologos.cc/logos/chainlink-link-logo.svg",
  },
  {
    value: "ATOMUSDT",
    label: "Cosmos",
    icon: "https://cryptologos.cc/logos/cosmos-atom-logo.svg",
  },
];

interface SymbolsComboboxProps {
  value?: string;
  onValueChange?: (value: string) => void;
}

export function SymbolsCombobox({
  value,
  onValueChange,
}: SymbolsComboboxProps) {
  const [open, setOpen] = React.useState(false);

  const renderSymbolValue = () => {
    const symbol = symbols.find((symbol) => symbol.value === value);

    if (!symbol) return "Select symbol...";

    return (
      <div className="flex items-center gap-1.5">
        <img className="size-6" src={symbol.icon} />
        <span>{symbol.label}</span>
      </div>
    );
  };

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <Button
          variant="outline"
          role="combobox"
          aria-expanded={open}
          className="w-[180px] justify-between"
        >
          {renderSymbolValue()}
          <ChevronsUpDown className="ml-2 h-4 w-4 shrink-0 opacity-50" />
        </Button>
      </PopoverTrigger>
      <PopoverContent className="w-[180px] p-0">
        <Command>
          <CommandInput placeholder="Search symbol..." />
          <CommandList>
            <CommandEmpty>No symbol found.</CommandEmpty>
            <CommandGroup>
              {symbols.map((symbol) => (
                <CommandItem
                  key={symbol.value}
                  value={symbol.value}
                  onSelect={(currentValue) => {
                    setOpen(false);

                    onValueChange?.(currentValue === value ? "" : currentValue);
                  }}
                >
                  <Check
                    className={cn(
                      "mr-2 size-4",
                      value === symbol.value ? "opacity-100" : "opacity-0",
                    )}
                  />
                  <div className="flex items-center gap-1.5">
                    <img className="size-5" src={symbol.icon} />
                    <span>{symbol.label}</span>
                  </div>
                </CommandItem>
              ))}
            </CommandGroup>
          </CommandList>
        </Command>
      </PopoverContent>
    </Popover>
  );
}

export default SymbolsCombobox;
