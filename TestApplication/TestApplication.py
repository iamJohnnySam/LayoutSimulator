
import socket
import time
import threading
from datetime import datetime

# List of commands to send. Each command is a tuple: (command, transaction_id, send_after_prev)
# 'send_after_prev' is a boolean indicating whether to wait for previous ack before sending.
commands = [
    ("COMMAND_1,12345", True, 2),  # This command waits for ack before sending the next
    ("COMMAND_2,67890", False, 2), # This command can be sent while processing the previous one
    # Add more commands as needed
]

connection: socket.socket = None
command_sent_time = datetime.now()
expected_success = None
return_count = 0;

def send_command(command):
    global connection
    connection.sendall(command.encode())
    print()
    print(f"{datetime.now().strftime('%H:%M:%S')}: {command}")
    

def receive_ack():
    global connection
    while (True):
        ack = connection.recv(1024).decode()  # Assuming ack fits in 1024 bytes
        recv_time = datetime.now()
        print(f"    {(recv_time - command_sent_time).total_seconds()}: {ack}")
        

def connect_app(port: int):
    global connection
    connection = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    connection.connect(('localhost', port))
    

def main():
    global connection

    for command, send_after_prev, expected_returns in commands:
        transaction_id = command.split(',')[0]

        if send_after_prev:
            time.sleep(10)
            # todo
            command_sent_time = datetime.now()
            
        send_command(command)


    time.sleep(10)
    connection.close()

if __name__ == "__main__":
    connect_app(8000)
    threading.Thread(target=receive_ack, daemon=True).start()
    time.sleep(1);
    main()
