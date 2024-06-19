import time
from concurrent import futures

import grpc
import ikpy.chain
import numpy as np

import ik_pb2
import ik_pb2_grpc


class IKService(ik_pb2_grpc.KinematicsProvider):
    def __init__(self, chain_l, chain_r):
        self.chain_r = chain_r
        self.chain_l = chain_l

    def CalculateInverseKinematicsRight(self, request, context, **kwargs):
        target_position, target_rotation, joints = self.extract_pos_rot(request)
        return self.do_ik(self.chain_r, target_position, target_rotation, joints)

    def CalculateInverseKinematicsLeft(self, request, context, **kwargs):
        target_position, target_rotation, joints = self.extract_pos_rot(request)
        return self.do_ik(self.chain_l, target_position, target_rotation, joints)

    def extract_pos_rot(self, request):
        target_position = request.target
        joints = request.currentJoints
        current_joints = [0.0, joints[0], joints[1], joints[2], joints[3], 0.0]
        target_rotation = [request.rotation.z, -request.rotation.x, request.rotation.y]
        position = [target_position.z, -target_position.x, target_position.y]  # Swap Y and Z because of Unity
        return position, target_rotation, current_joints

    def do_ik(self, chain, target_position, target_rotation, joints):
        angles_degrees = np.degrees(chain.inverse_kinematics(target_position=target_position,
                                                             initial_position=np.radians(joints),
                                                             # target_orientation=target_rotation,
                                                             # orientation_mode="Y"
                                                             ))

        angles_degrees = angles_degrees[1:-1]
        ik_response = ik_pb2.IKResponse()
        ik_response.jointTargets.extend(angles_degrees)
        return ik_response


def serve():
    chain_l = ikpy.chain.Chain.from_urdf_file("arm_l.urdf", active_links_mask=[False, True, True, True, False, False])
    chain_r = ikpy.chain.Chain.from_urdf_file("arm_r.urdf", active_links_mask=[False, True, True, True, False, False])
    server = grpc.server(futures.ThreadPoolExecutor(max_workers=256))
    ik_pb2_grpc.add_KinematicsProviderServicer_to_server(IKService(chain_l, chain_r), server)

    server.add_insecure_port('[::]:50051')
    server.start()
    print("Server started, listening on port 50051.")  # Confirm server started
    try:
        while True:
            time.sleep(86400)
    except KeyboardInterrupt:
        print("Server stopping...")  # Confirm server stopping
        server.stop(0)


if __name__ == '__main__':
    serve()
