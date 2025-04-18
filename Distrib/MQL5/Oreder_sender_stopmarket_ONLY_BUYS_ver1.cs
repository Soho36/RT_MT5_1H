#property version   "1.04"
#property script_show_inputs

input string pattern="";
input double volume=1;
input string comment="";
datetime last_bar_time = 0;

//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+

void OnTick()
{	
	static datetime Old_Time = 0;
    datetime current_bar_time = iTime(_Symbol, _Period, 0); // current bar (which may still be forming)

    if (current_bar_time != last_bar_time)
    {
        // This is the *first tick* of a new candle
        last_bar_time = current_bar_time;

        // 🧠 Place logic here: this will run ONCE per candle, right after close
        // Use iClose(_Symbol, _Period, 1) for just-closed candle
        double closed_candle_close = iClose(_Symbol, _Period, 1);
        Print("Just-closed candle value: ", closed_candle_close);

        // ✅ Now you can place pending orders, update TP, etc.
    }

    // Main trading logic goes here
    // === Check for open BUY position ===
   if (PositionSelect(_Symbol))
   {
       ulong ticket       = PositionGetTicket(_Symbol);
       double entry_price = PositionGetDouble(POSITION_PRICE_OPEN);
       double stop_loss   = PositionGetDouble(POSITION_SL);
       double take_profit = PositionGetDouble(POSITION_TP);
       double current_close;

       // Calculate RISK
       double risk = MathAbs(entry_price - stop_loss) > (_Point * 2);

       // Get last candle close (not the forming one)
       double close_price[2];
       if (CopyClose(_Symbol, _Period, 1, 1, close_price) <= 0)
       {
           Print("Failed to get candle close: ", GetLastError());
           return;
       }
       current_close = close_price[0];

       // For BUY: look for price moving up far enough
       if (current_close - entry_price >= risk && MathAbs(take_profit - current_close) > _Point)
       {
           // Update TP
           MqlTradeRequest request = {};
           MqlTradeResult result = {};

           request.action   = TRADE_ACTION_SLTP;
           request.symbol   = _Symbol;
           request.sl       = stop_loss;
           request.tp       = current_close;
           request.position = ticket;

           if (!OrderSend(request, result))
           {
               Print("Failed to update TP: ", GetLastError());
           }
           else
           {
               Print("BUY TP updated to: ", current_close);
           }
       }
   }

    #define EXPERT_MAGIC 123456
    string fileName = "buy_signals_from_python.txt";

    // Check if the file exists
    if (!FileIsExist(fileName))
    {
        Print("File does not exist: ", fileName);
        return;
    }

    Sleep(500); // Adjust sleep time if needed

    // Open and read file
    int fileHandle = FileOpen(fileName, FILE_READ | FILE_ANSI);
    if (fileHandle == INVALID_HANDLE)
    {
        Print("Failed to open file: ", GetLastError());
        return;
    }

    // Read signal from file
    ulong fileSize = FileSize(fileHandle);
    string signal = "";
    if (fileSize > 0)
    {
        signal = FileReadString(fileHandle, fileSize);
    }
    FileClose(fileHandle);  // Close immediately after reading

    // Clear the file
    fileHandle = FileOpen(fileName, FILE_WRITE | FILE_ANSI);
    if (fileHandle != INVALID_HANDLE)
    {
        FileWriteString(fileHandle, "");  // Clear content immediately
        FileClose(fileHandle);  // Close after clearing
    }

    // Process the signal
    if (signal != "")
    {
        string components[];
        int count = StringSplit(signal, ',', components);
        if (count >= 4)
        {
            string symbol = components[0];
            string direction = components[1];
            double stop_market = StringToDouble(components[2]);
            double stop_loss = StringToDouble(components[3]);
            double take_profit = StringToDouble(components[4]);
            // double volume = 1;

            // FILE CONTENTS MUST LOOK LIKE THIS:
            // BTCUSD,Sell,84600,85000,84000
            Print(symbol, ", ", direction, ", ", stop_market, ", ", stop_loss, ", ", take_profit);

            MqlTradeRequest request = {};
            MqlTradeResult result = {};

            request.action = TRADE_ACTION_PENDING;
            request.symbol = symbol;
            request.volume = volume;
            request.sl = stop_loss;
            request.tp = take_profit;
            request.deviation = 5;
            request.magic = EXPERT_MAGIC;
            request.comment = "250324";

            if (direction == "Buy")
            {
                request.type = ORDER_TYPE_BUY_STOP;
                request.price = stop_market;
            }

            else
            {
                Print("Invalid direction: ", direction);
                return; // Exit if the direction is invalid
            }
            // Cancel existing SELL STOP pending orders with same magic number
			for (int i = OrdersTotal() - 1; i >= 0; i--)
         {
             ulong ticket = OrderGetTicket(i);
             Print("Checking order #", i, ", ticket: ", ticket);

             if (!OrderSelect(ticket))
             {
                 Print("Failed to select order with ticket: ", ticket, ", error: ", GetLastError());
                 continue;
             }

             string orderSymbol = OrderGetString(ORDER_SYMBOL);
             int orderMagic     = (int)OrderGetInteger(ORDER_MAGIC);
             int orderType      = (int)OrderGetInteger(ORDER_TYPE);

             Print("Order info - Symbol: ", orderSymbol, ", Magic: ", orderMagic, ", Type: ", orderType);

             if (orderSymbol != symbol)
             {
                 Print("Skipped: symbol mismatch");
                 continue;
             }

             if (orderMagic != EXPERT_MAGIC)
             {
                 Print("Skipped: magic number mismatch");
                 continue;
             }

             if (orderType != ORDER_TYPE_BUY_STOP)
             {
                 Print("Skipped: not a SELL STOP");
                 continue;
             }

             // Cancel the existing pending order
             MqlTradeRequest cancel = {};
             MqlTradeResult cancel_result = {};

             cancel.action = TRADE_ACTION_REMOVE;
             cancel.order = ticket;

             if (!OrderSend(cancel, cancel_result))
             {
                 Print("Failed to cancel order: ", GetLastError());
             }
             else
             {
                 Print("Cancelled existing SELL STOP order, ticket: ", ticket);
             }
         }


            if (!OrderSend(request, result))
            {
                Print("OrderSend error ", GetLastError());
            }
            else
            {
                PrintFormat("Request: action=%d, symbol=%s, volume=%.2f, price=%.2f, sl=%.2f, tp=%.2f, type=%d",
            request.action, request.symbol, request.volume, request.price, request.sl, request.tp, request.type);
            }
        }
    }
}