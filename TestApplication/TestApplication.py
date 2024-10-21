import time

import tcpCommands
import grpcCommands


if __name__ == "__main__":
    
    tcpCommands.run_commands()
    # grpcCommands.run_commands()


    while(1):
        time.sleep(1);
