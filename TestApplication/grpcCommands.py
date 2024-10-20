import grpc
import command_pb2
import command_pb2_grpc
import asyncio
from datetime import datetime


def run_commands():

    asyncio.run(execute(command_pb2.Job(action = command_pb2.StartSim)))
    pod = asyncio.run(execute(command_pb2.Job(action = command_pb2.Pod, arguments={command_pb2.Capacity: "25", command_pb2.Type: "payload"})))
    asyncio.run(execute(command_pb2.Job(action = command_pb2.Payload, arguments={command_pb2.Slot: "1"})))
    asyncio.run(execute(command_pb2.Job(action = command_pb2.Payload, arguments={command_pb2.Slot: "5"})))
    asyncio.run(execute(command_pb2.Job(action = command_pb2.Payload, arguments={command_pb2.Slot: "23"})))
    asyncio.run(execute(command_pb2.Job(action = command_pb2.Payload, arguments={command_pb2.Slot: "25"})))
    asyncio.run(execute(command_pb2.Job(action = command_pb2.Dock, target = "L1", arguments={command_pb2.PodID: pod})))
    asyncio.run(execute(command_pb2.Job(action = command_pb2.Map, target = "L1")))
    asyncio.run(execute(command_pb2.Job(action = command_pb2.PowerOn, target = "R1")))
    asyncio.run(execute(command_pb2.Job(action = command_pb2.Pick, target = "R1", arguments={command_pb2.EndEffector: "1", command_pb2.TargetStation: "L1", command_pb2.Slot: "3"})))
    asyncio.run(execute(command_pb2.Job(action = command_pb2.Pick, target = "R1", arguments={command_pb2.EndEffector: "1", command_pb2.TargetStation: "L1", command_pb2.Slot: "1"})))
    asyncio.run(execute(command_pb2.Job(action = command_pb2.Place, target = "R1", arguments={command_pb2.EndEffector: "1", command_pb2.TargetStation: "A1", command_pb2.Slot: "1"})))
    asyncio.run(execute(command_pb2.Job(action = command_pb2.Door, target = "L1", arguments={command_pb2.DoorStatus: "1"})))
    asyncio.run(execute(command_pb2.Job(action = command_pb2.Map, target = "L1")))
    asyncio.run(execute(command_pb2.Job(action = command_pb2.Process0, target = "A1")))
    asyncio.run(execute(command_pb2.Job(action = command_pb2.Pick, target = "R1", arguments={command_pb2.EndEffector: "1", command_pb2.TargetStation: "A1", command_pb2.Slot: "1"})))
    asyncio.run(execute(command_pb2.Job(action = command_pb2.DoorOpen, target = "P1")))
    asyncio.run(execute(command_pb2.Job(action = command_pb2.Place, target = "R1", arguments={command_pb2.EndEffector: "1", command_pb2.TargetStation: "P1", command_pb2.Slot: "1"})))
    asyncio.run(execute(command_pb2.Job(action = command_pb2.DoorClose, target = "P1")))
    asyncio.run(execute(command_pb2.Job(action = command_pb2.Process0, target = "P1")))
    asyncio.run(execute(command_pb2.Job(action = command_pb2.Pick, target = "R1", arguments={command_pb2.EndEffector: "1", command_pb2.TargetStation: "L1", command_pb2.Slot: "5"})))
    asyncio.run(execute(command_pb2.Job(action = command_pb2.DoorOpen, target = "P1")))
    asyncio.run(execute(command_pb2.Job(action = command_pb2.Pick, target = "R1", arguments={command_pb2.EndEffector: "2", command_pb2.TargetStation: "P1", command_pb2.Slot: "1"})))
    asyncio.run(execute(command_pb2.Job(action = command_pb2.Place, target = "R1", arguments={command_pb2.EndEffector: "1", command_pb2.TargetStation: "P1", command_pb2.Slot: "1"})))




async def execute(job):
    async with grpc.aio.insecure_channel('localhost:50051') as channel:
        stub = command_pb2_grpc.LayoutSimulatorStub(channel)

        t = datetime.now()
        print()
        print(f"{t.strftime('%H:%M:%S')} : Command -> {job.action} | Target -> {job.target}.")

        response = await stub.ExecuteCommand_GRPC(job)

        if response.responseType == 0:
            m = "ACK"
        elif response.responseType == 1:
            m = "NACK"
        elif response.responseType == 2:
            m = "SUCCESS"
        else:
            m = "ERROR"

        print(f"    {(datetime.now() - t).total_seconds()}: Response -> {m} | Message -> {response.response}.")
        return response.response