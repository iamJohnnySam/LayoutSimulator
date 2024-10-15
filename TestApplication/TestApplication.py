
import socket
import time
import threading
from datetime import datetime

commands = [
    ("<001,POD,25,wafer>\r\n", True, 2),
    ("<002,PAYLOAD,,1>\r\n", True, 2),
    ("<003,PAYLOAD,,5>\r\n", True, 2),
    ("<004,PAYLOAD,,23>\r\n", True, 2),
    ("<005,PAYLOAD,,25>\r\n", True, 2),
    ("<006,DOCK,LP1>\r\n", True, 2),
    ("<007:LoadandMap,LP1>\r\n", True, 2),
    ("<008:ServoOn,R1>\r\n", False, 2),
    ("<009:RobotPick,R1,1,LP1,1>\r\n", True, 2),
    ("<010:RobotPlace,R1,1,A1,1>\r\n", True, 2),
    ("<011:Remap,LP1>\r\n", False, 2),
    ("<012:align,A1,90>\r\n", True, 2),
    ("<013:RobotPick,R1,1,A1,1>\r\n", True, 2),
    ("<014:dooropen,PM1>\r\n", False, 2),
    ("<015:RobotPlace,R1,1,PM1,1>\r\n", True, 2),
    ("<016:doorclose,PM1>\r\n", True, 2),
    ("<017:RobotPick,R1,1,LP1,1>\r\n", True, 2),
    ("<018:process,PM1>\r\n", False, 2),
    ("<019:RobotPick,R1,1,LP1,2>\r\n", True, 2),
    ("<020:RobotPick,R1,1,LP1,5>\r\n", True, 2),
    ("<021:RobotPlace,R1,1,A1,1>\r\n", True, 2),
    ("<022:align,A1,90>\r\n", True, 2),
    ("<023:RobotPick,R1,1,A1,1>\r\n", True, 2),
    
]

connection: socket.socket = None
command_sent_time = datetime.now()

expected_success_tID = None
expected_success: bool = False

return_count = 0;
expected_returns = 0;

def send_command(command):
    global connection
    connection.sendall(command.encode())
    print()
    print(f"{datetime.now().strftime('%H:%M:%S')}: {command.replace('\n', '').replace('\r', '')}")
    

def receive_ack():
    global connection
    global expected_success
    global expected_success_tID
    global return_count
    global expected_returns

    while (True):
        try:
            ack = connection.recv(1024).decode()
        except:
            break

        recv_time = datetime.now()
        print(f"    {(recv_time - command_sent_time).total_seconds()}: {ack.replace('\n', '').replace('\r', '')}")
        if expected_success_tID == get_transaction_id(ack):
            return_count = return_count + 1
            if expected_returns == return_count:
                expected_success = False
                return_count = 0
        

def connect_app(port: int):
    global connection
    connection = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    connection.connect(('localhost', port))

def get_transaction_id(command):
    return command.replace(':', ',').split(",")[0]
    

def main():
    global connection
    global command_sent_time
    global expected_success
    global expected_success_tID
    global expected_returns

    timed_out = False

    for command, wait_to_send, _expected_returns in commands:
        transaction_id = command.split(',')[0]

        x = 0
        if wait_to_send:
            while (expected_success):
                time.sleep(1)
                x = x + 1
                if x > 60:
                    expected_success = True
                    timed_out = True

            if not timed_out:
                expected_success_tID = get_transaction_id(command)
                command_sent_time = datetime.now()
                expected_returns = _expected_returns
                expected_success = True
            
        if not timed_out: send_command(command)

    while (expected_success):
        time.sleep(1)

    print()
    print()
    print("SEQUENCE COMPLETE")

    connection.close()


if __name__ == "__main__":
    connect_app(8000)
    threading.Thread(target=receive_ack, daemon=True).start()
    time.sleep(1);
    main()
    while(1):
        time.sleep(1);
