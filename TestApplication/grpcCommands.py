import grpc
import command_pb2
import command_pb2_grpc
import asyncio

commands = [
    ("<007:LoadandMap,L1>\r\n", True, 2),
    ("<008:ServoOn,R1>\r\n", False, 2),
    ("<009:RobotPick,R1,1,L1,1>\r\n", True, 2),
    ("<010:RobotPlace,R1,1,A1,1>\r\n", True, 2),
    ("<011:Remap,L1>\r\n", False, 2),
    ("<012:align,A1,90>\r\n", True, 2),
    ("<013:RobotPick,R1,1,A1,1>\r\n", True, 2),
    ("<014:dooropen,P1>\r\n", False, 2),
    ("<015:RobotPlace,R1,1,P1,1>\r\n", True, 2),
    ("<016:doorclose,P1>\r\n", True, 2),
    ("<017:RobotPick,R1,1,L1,1>\r\n", True, 2),
    ("<018:process,P1>\r\n", False, 2),
    ("<019:RobotPick,R1,1,L1,2>\r\n", True, 2),
    ("<020:RobotPick,R1,1,L1,5>\r\n", True, 2),
    ("<021:RobotPlace,R1,1,A1,1>\r\n", True, 2),
    ("<022:align,A1,90>\r\n", True, 2),
    ("<023:RobotPick,R1,1,A1,1>\r\n", True, 2),
]

def run_commands():
    asyncio.run(execute(command_pb2.Job(action = command_pb2.Pod, capacity=25, payloadType="wafer")))
    asyncio.run(execute(command_pb2.Job(action = command_pb2.Payload, slot=1)))
    asyncio.run(execute(command_pb2.Job(action = command_pb2.Payload, slot=5)))
    asyncio.run(execute(command_pb2.Job(action = command_pb2.Payload, slot=23)))
    asyncio.run(execute(command_pb2.Job(action = command_pb2.Payload, slot=25)))
    asyncio.run(execute(command_pb2.Job(action = command_pb2.Dock, target="L1")))
    asyncio.run(execute(command_pb2.Job(action = command_pb2.Map, target="L1")))
    asyncio.run(execute(command_pb2.Job(action = command_pb2.PowerOn, target="R1")))



    

    job = command_pb2.Job(
        action=command_pb2.Pick,
        transactionID="12345",
        target="Station1",
        endEffector=1,
        slot=2,
        podID="POD123",
        state=True,
        capacity=10,
        payloadType="TypeA",
        targetStation="StationA"
    )
 

async def execute(job):
    async with grpc.aio.insecure_channel('localhost:50051') as channel:
        stub = command_pb2_grpc.LayoutSimulatorStub(channel)
        response = await stub.ExecuteSimCommand(job)
        print(f"Response: {response.responseType}, {response.response}")