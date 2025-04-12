import pandas as pd
from datetime import datetime, timedelta

log_file_reading_interval = 1       # File reading interval (sec)

# +------------------------------------------------------------------+
# FILE TRANSMIT PATHS
# +------------------------------------------------------------------+

mt5_account_number = 475    # LAST 3 DIGITS OF MT5 ACCOUNT. MUST BE CHANGED BEFORE BUILDING EXE

# MT5 directory with OHLC log file (logging on active timeframe):

# ------------------------
# LIIKURI PATHS # HASH FOLDER MUST BE CHANGED BEFORE EXE BUILD
# mt5_logging_file_path = (
#     f'C:\\Users\\Liikurserv\\AppData\\Roaming\\MetaQuotes\\Terminal\\1D0E83E0BCAA42603583233CF21A762C\\MQL5\\Files\\'
#     f'OHLCVData_{mt5_account_number}.csv'
# )
# File with signal generated by Python script
# buy_sell_signals_for_mt5_filepath_1 = (
#      f'C:\\Users\\Liikurserv\\AppData\\Roaming\\MetaQuotes\\Terminal\\1D0E83E0BCAA42603583233CF21A762C\\MQL5\\Files\\'
#      f'buy_sell_signals_from_python_1.txt'
#      )
#
# buy_sell_signals_for_mt5_filepath_2 = (
#      f'C:\\Users\\Liikurserv\\AppData\\Roaming\\MetaQuotes\\Terminal\\1D0E83E0BCAA42603583233CF21A762C\\MQL5\\Files\\'
#      f'buy_sell_signals_from_python_2.txt'
#      )

levels_path = (
    f'C:\\Users\\Liikurserv\\PycharmProjects\\RT_MT5\\hardcoded_sr_levels.csv'
)

list_of_orders_path = 'list_of_orders.csv'
position_state_path = (
    'C:\\Users\\Liikurserv\\AppData\\Roaming\\MetaQuotes\\Terminal\\1D0E83E0BCAA42603583233CF21A762C\\MQL5\\Files\\PositionState.txt'
)
# SILLAMAE PATHS
mt5_logging_file_path = (
    f'C:\\Users\\Vova deduskin lap\\AppData\\Roaming\\MetaQuotes\\Terminal\\'
    f'D0E8209F77C8CF37AD8BF550E51FF075\\MQL5\\Files\\OHLCVData_{mt5_account_number}.csv'
)

# File with signal generated by Python script
buy_sell_signals_for_mt5_filepath = (
     f'C:\\Users\\Vova deduskin lap\\AppData\\Roaming\\MetaQuotes\\Terminal\\'
     f'D0E8209F77C8CF37AD8BF550E51FF075\\MQL5\\Files\\buy_sell_signals_from_python.txt'
     )


def leave_only_last_line():     # Clear file before starting the script
    with open(mt5_logging_file_path, 'r', encoding='utf-16') as file:
        lines = file.readlines()
        # Check if there's at least one line to keep
        if lines:
            with open(mt5_logging_file_path, 'w', encoding='utf-16')as file:
                file.write(lines[-1])  # Write only the first line back to the file


def get_dataframe_from_file(max_time_waiting_for_entry):

    log_df = pd.read_csv(
        mt5_logging_file_path,
        sep=';',
        encoding='utf-16',
        engine='python',
        # skiprows=lambda x: x < (sum(1 for _ in open(mt5_logging_file_path, encoding='utf-16')) - max_time_waiting_for_entry - 1
        # )
    )
    new_column_names = ['Ticker', 'Timeframe', 'Date', 'Time', 'Open', 'High', 'Low', 'Close', 'Volume']
    log_df.columns = new_column_names
    log_df['Datetime'] = pd.to_datetime(log_df['Date'] + ' ' + log_df['Time'], format='ISO8601')
    log_df.set_index('Datetime', inplace=True)
    dataframe_from_log = log_df.loc[:, ['Ticker', 'Date', 'Time', 'Open', 'High', 'Low', 'Close']]
    datetime_index = log_df.index
    last_date = str(datetime_index[-1])     # Get datetime of the second row of dataframe to pass along with levels

    return dataframe_from_log, last_date


# def get_levels_from_file():
#     with open(levels_path, 'r', encoding='utf-8') as file:
#         levels = [
#             (line.split(',')[0].strip(), float(line.split(',')[1].strip()))
#             for line in file
#         ]
#     return levels


def get_levels_from_file():
    updated_lines = []
    levels = []

    with open(levels_path, 'r', encoding='utf-8') as file:
        for line in file:
            parts = line.strip().split(',')

            if len(parts) == 2:
                # Properly formatted line
                timestamp = parts[0].strip()
                level = float(parts[1].strip())
            else:
                # Line with only a level; add current timestamp
                current_time = datetime.now()

                if current_time.second > 0:
                    current_time += timedelta(minutes=1)

                current_time = current_time.replace(second=0, microsecond=0)

                timestamp = current_time.strftime('%Y-%m-%d %H:%M:%S')
                level = float(parts[0].strip())

            # Add the formatted line to the update list
            updated_lines.append(f"{timestamp}, {level}\n")
            levels.append((timestamp, level))

    # Rewrite the file with only properly formatted lines
    with open(levels_path, 'w', encoding='utf-8') as file:
        file.writelines(updated_lines)

    return levels


#   Remove level which has reached time threshold from file
def remove_expired_levels(level_lifetime_minutes, dataframe_from_log):
    # output_df_with_levels = output_df_with_levels.set_index(['Datetime'], inplace=True)
    current_time = dataframe_from_log.index[-1]  # Timestamp of the last line of dataframe
    updated_levels = []

    with open(levels_path, 'r', encoding='utf-8') as file:
        for line in file:
            timestamp_str, level = line.strip().split(',')
            timestamp = datetime.strptime(timestamp_str, "%Y-%m-%d %H:%M:%S")

            # Calculate the time difference
            time_diff = (current_time - timestamp).total_seconds() / 60  # Convert to minutes

            # Keep the level if it is still within its lifetime
            if time_diff < level_lifetime_minutes:
                updated_levels.append(line)
            else:
                print(f"Removing expired level: {timestamp_str}, {level.strip()}")

    # Write the remaining levels back to the file
    with open(levels_path, 'w', encoding='utf-8') as file:
        file.writelines(updated_levels)


def get_position_state():
    with open(position_state_path, 'r', encoding='mbcs') as file:
        state = file.read()
        print(state)
        return state


# Create file for orders exchange between Python and MT5
def save_order_parameters_to_file(line_order_parameters):   # Called from orders_sender.py
    with open(buy_sell_signals_for_mt5_filepath, 'w', encoding='utf-8') as file:
        file.writelines(line_order_parameters)
        print('NEW ORDER IS SUCCESSFULLY SAVED TO FILE_1')

    # with open(buy_sell_signals_for_mt5_filepath_2, 'w', encoding='utf-8') as file:
    #     file.writelines(line_order_parameters)
    #     print('NEW ORDER IS SUCCESSFULLY SAVED TO FILE_2')


# Create orders list file to track orders
def save_list_of_orders_to_file(line_order_parameters_to_order_list):
    with open(list_of_orders_path, 'w', encoding='utf-8') as file:
        file.writelines(line_order_parameters_to_order_list)


def get_last_order_time_from_file():
    with open(list_of_orders_path, 'r', encoding='utf-8') as file:
        last_order_timestamp = pd.to_datetime(file.read())
        if pd.isna(last_order_timestamp):
            last_order_timestamp = pd.to_datetime('2024-01-01 00:00:00')  # Default value while the file is empty

        return last_order_timestamp
