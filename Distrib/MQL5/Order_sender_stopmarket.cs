#property version   "1.03"
#property script_show_inputs

input string pattern="";
input double volume=1;
input string comment="";
input int magic=12345;


//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+

void OnTick()
{
    static datetime Old_Time = 0;
    datetime New_Time[1];

    // Check if there is a new bar
    int copied = CopyTime(_Symbol, _Period, 0, 1, New_Time);
    if (copied <= 0)
    {
        Alert("Error in copying historical times data, error =", GetLastError());
        ResetLastError();
        return;
    }

    // Only proceed if the bar time has changed
    if (Old_Time == New_Time[0]) return;

    Old_Time = New_Time[0]; // Update Old_Time to prevent re-triggering within the same bar

    // Main trading logic goes here
    #define EXPERT_MAGIC 123456
    string fileName = "buy_sell_signals_from_python.txt";

    // Check if the file exists
    if (!FileIsExist(fileName))
    {
        Print("File does not exist: ", fileName);
        return;
    }

    Sleep(1000); // Adjust sleep time if needed

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
            else if (direction == "Sell")
            {
                request.type = ORDER_TYPE_SELL_STOP;
                request.price = stop_market;
            }
            else
            {
                Print("Invalid direction: ", direction);
                return; // Exit if the direction is invalid
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