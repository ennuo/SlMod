namespace SlLib.Utilities;

public static class SkeletonUtil
{
    public static string MapSekaiSkeleton(List<(string Bone, int Parent)> joints, int index)
    {
        while (true)
        {
            if (index == -1) return string.Empty;
            (string? name, int parent) = joints[index];
            switch (name)
            {
                // Main body root, treat it as the pelvis
                case "kl_mune_b_wj": return "Chest";
                
                // Hair and head
                case "n_kubi_wj_ex": // Neck
                case "j_kao_wj": // Head
                    return "Neck";
                
                // This can't possibly go wrong
                case "n_tail_l_a_wj_ex": 
                case "n_tail_l_b_wj_ex":
                case "n_tail_r_a_wj_ex":
                case "n_tail_r_b_wj_ex":
                    return "Hair_01";
                
                case "j_tail_l_000_wj":
                case "j_tail_r_000_wj": 
                    return "Hair_02";
                
                case "j_opai_l_000wj":
                case "j_opai_r_000wj":
                case "n_kubi_wj_ex_ragdoll":
                case "j_necktie_000_wj": 
                    return "Chest";
                
                case "n_hara_b_wj_ex_ragdoll":
                case "n_hara_c_wj_ex_ragdoll":
                    return "Waist";
                
                // Legs
                case "kl_kosi_etc_ragdoll": return "Hips";
                case "n_momo_a_l_wj_cd_ex_ragdoll": return "LeftThigh";
                case "n_momo_a_r_wj_cd_ex_ragdoll": return "RightThigh";
                case "j_momo_l_ragdoll": return "LeftKnee";
                case "j_momo_r_ragdoll": return "RightKnee";
                case "kl_asi_l_wj_co_ragdoll": return "LeftFoot";
                case "kl_toe_l_ragdoll": return "LeftToe";
                case "kl_asi_r_wj_co_ragdoll": return "RightFoot";
                case "kl_toe_r_ragdoll": return "RightToe";
                case "n_toe_l_wj_ex": return "LeftToe";
                case "n_toe_r_wj_ex": return "RightToe";
                
                // Arms
                case "kl_waki_l_ragdoll": return "LeftCollar";
                case "n_waki_x_l_wj_ex": return "LeftCollar";
                case "n_skata_l_wj_cd_ex_ragdoll": return "LeftShoulder";
                case "j_kata_l_wj_cu_ragdoll": return "LeftShoulder";
                case "j_ude_l_ragdoll": return "LeftElbow";
                
                case "kl_waki_r_ragdoll": return "RightCollar";
                case "n_waki_x_r_wj_ex": return "RightCollar";
                case "n_skata_r_wj_cd_ex_ragdoll": return "RightShoulder";
                case "j_kata_r_wj_cu_ragdoll": return "RightShoulder";
                case "j_ude_r_ragdoll": return "RighElbow";
                
                
                
                
                // case "kl_waki_l_ragdoll": return "LeftCollar";
                // case "j_kata_l_wj_cu_ragdoll": return "LeftShoulder";
                // case "j_ude_l_ragdoll": return "LeftElbow";
                //
                // case "kl_waki_r_ragdoll": return "RightCollar";
                // case "j_kata_r_wj_cu_ragdoll": return "RightShoulder";
                // case "j_ude_r_ragdoll": return "RighElbow";
                
                // Hands
                case "kl_te_l_wj": return "LeftHand";
                case "kl_te_r_wj": return "RightHand";
                
                default:
                    index = parent;
                    continue;
            }
        }
    }
        
    public static string MapEggmanNegaSkeleton(List<(string Bone, int Parent)> joints, int index)
    {
        while (true)
        {
            if (index == -1) return string.Empty;
            (string? name, int parent) = joints[index];
            switch (name)
            {
                case "bone_root": return "Pelvis";
                
                case "Pelvis": return "Hips";
                case "Hips": return "Pelvis";
                
                case "LeftUpLeg": return "LeftThigh";
                case "LeftLeg": return "LeftKnee";
                case "LeftFoot": return "LeftFoot";
                case "LeftToeBase": return "LeftBall";
                
                case "RightUpLeg": return "RightThigh";
                case "RightLeg": return "RightKnee";
                case "RightFoot": return "RightFoot";
                case "RightToeBase": return "RightBall";
                
                case "Spine": return "Spine";
                case "Spine1": return "Chest";
                
                case "LeftShoulder": return "LeftCollar";
                case "LeftArm": return "LeftShoulder";
                case "LeftForeArm": return "LeftElbow";
                case "LeftHand": return "LeftHand";
                case "LeftHandIndex1": return "LeftIndex_01";
                case "LeftHandIndex2": return "LeftIndex_02";
                case "LeftHandIndex3": return "LeftIndex_03";
                case "LeftHandMiddle1": return "LeftMiddle_01";
                case "LeftHandMiddle2": return "LeftMiddle_02";
                case "LeftHandMiddle3": return "LeftMiddle_03";
                case "LeftHandRing1": return "LeftRing_01";
                case "LeftHandRing2": return "LeftRing_02";
                case "LeftHandRing3": return "LeftRing_03";
                case "LeftHandPinky1": return "LeftLittle_01";
                case "LeftHandPinky2": return "LeftLittle_02";
                case "LeftHandPinky3": return "LeftLittle_03";
                case "LeftHandThumb1": return "LeftThumb_01";
                case "LeftHandThumb2": return "LeftThumb_02";
                case "LeftHandThumb3": return "LeftThumb_03";
                
                case "RightShoulder": return "RightCollar";
                case "RightArm": return "RightShoulder";
                case "RightForeArm": return "RightElbow";
                case "RightHand": return "RightHand";
                case "RightHandIndex1": return "RightIndex_01";
                case "RightHandIndex2": return "RightIndex_02";
                case "RightHandIndex3": return "RightIndex_03";
                case "RightHandMiddle1": return "RightMiddle_01";
                case "RightHandMiddle2": return "RightMiddle_02";
                case "RightHandMiddle3": return "RightMiddle_03";
                case "RightHandRing1": return "RightRing_01";
                case "RightHandRing2": return "RightRing_02";
                case "RightHandRing3": return "RightRing_03";
                case "RightHandPinky1": return "RightLittle_01";
                case "RightHandPinky2": return "RightLittle_02";
                case "RightHandPinky3": return "RightLittle_03";
                case "RightHandThumb1": return "RightThumb_01";
                case "RightHandThumb2": return "RightThumb_02";
                case "RightHandThumb3": return "RightThumb_03";
                
                case "Neck": return "Neck";
                
                default:
                    index = parent;
                    continue;
            }
        }
    }
    
    public static string MapFortniteMediumSkeleton(List<(string Bone, int Parent)> joints, int index)
    {
        while (true)
        {
            if (index == -1) return string.Empty;
            (string? name, int parent) = joints[index];

            switch (name)
            {
                case "root":
                case "pelvis":
                    return "Pelvis";
                case "neck_01":
                case "neck_02":
                    return "Neck";
                // case "head": return "Neck";
                case "head":
                    return "Head";
                case "spine_01":
                    return "Hips";
                case "spine_02":
                case "spine_03":
                case "spine_04":
                case "spine_05":
                    return "Chest";

                case "clavicle_l":
                    return "LeftCollar";
                case "upperarm_twist_01_l":
                case "upperarm_twist_02_l":
                case "upperarm_l":
                    return "LeftShoulder";
                case "lowerarm_twist_01_l":
                case "lowerarm_twist_02_l":
                case "lowerarm_l":
                    return "LeftElbow";
                case "index_metacarpal_l":
                case "middle_metacarpal_l":
                case "pinky_metacarpal_l":
                case "ring_metacarpal_l":
                case "hand_l":
                    return "LeftHand";
                case "index_01_l":
                    return "LeftIndex_01";
                case "index_02_l":
                    return "LeftIndex_02";
                case "index_03_l":
                    return "LeftIndex_03";
                case "pinky_01_l":
                    return "LeftLittle_01";
                case "pinky_02_l":
                    return "LeftLittle_02";
                case "pinky_03_l":
                    return "LeftLittle_03";
                case "middle_01_l":
                    return "LeftMiddle_01";
                case "middle_02_l":
                    return "LeftMiddle_02";
                case "middle_03_l":
                    return "LeftMiddle_03";
                case "ring_01_l":
                    return "LeftRing_01";
                case "ring_02_l":
                    return "LeftRing_02";
                case "ring_03_l":
                    return "LeftRing_03";
                case "thumb_01_l":
                    return "LeftThumb_01";
                case "thumb_02_l":
                    return "LeftThumb_02";
                case "thumb_03_l":
                    return "LeftThumb_03";


                case "clavicle_r":
                    return "RightCollar";
                case "upperarm_twist_01_r":
                case "upperarm_twist_02_r":
                case "upperarm_r":
                    return "RightShoulder";
                case "lowerarm_twist_01_r":
                case "lowerarm_twist_02_r":
                case "lowerarm_r":
                    return "RightElbow";
                case "index_metacarpal_r":
                case "middle_metacarpal_r":
                case "pinky_metacarpal_r":
                case "ring_metacarpal_r":
                case "hand_r":
                    return "RightHand";
                case "index_01_r":
                    return "RightIndex_01";
                case "index_02_r":
                    return "RightIndex_02";
                case "index_03_r":
                    return "RightIndex_03";
                case "pinky_01_r":
                    return "RightLittle_01";
                case "pinky_02_r":
                    return "RightLittle_02";
                case "pinky_03_r":
                    return "RightLittle_03";
                case "middle_01_r":
                    return "RightMiddle_01";
                case "middle_02_r":
                    return "RightMiddle_02";
                case "middle_03_r":
                    return "RightMiddle_03";
                case "ring_01_r":
                    return "RightRing_01";
                case "ring_02_r":
                    return "RightRing_02";
                case "ring_03_r":
                    return "RightRing_03";
                case "thumb_01_r":
                    return "RightThumb_01";
                case "thumb_02_r":
                    return "RightThumb_02";
                case "thumb_03_r":
                    return "RightThumb_03";

                case "thigh_l":
                case "thigh_twist_01_l":
                    return "LeftThigh";
                case "calf_l":
                case "calf_twist_01_l":
                case "calf_twist_02_l":
                    return "LeftKnee";
                case "foot_l":
                    return "LeftFoot";
                case "ball_l":
                    return "LeftToe";
                case "thigh_r":
                case "thigh_twist_01_r":
                    return "RightThigh";
                case "calf_r":
                case "calf_twist_01_r":
                case "calf_twist_02_r":
                    return "RightKnee";
                case "foot_r":
                    return "RightFoot";
                case "ball_r":
                    return "RightToe";
                default:
                {
                    if (name.EndsWith("_end"))
                    {
                        string parentName = name[..name.LastIndexOf("_end", StringComparison.Ordinal)];
                        index = joints.FindIndex(joint => joint.Bone == parentName);
                        if (index == -1) return string.Empty;
                        continue;
                    }

                    index = parent;
                    continue;
                }
            }
        }
    }

    public static string MapKiryuSkeleton(List<(string Bone, int Parent)> joints, int index)
    {
        while (true)
        {
            if (index == -1) return string.Empty;
            (string? name, int parent) = joints[index];

            switch (name)
            {
                case "center_c_n":
                case "vector_c_n":
                case "pattern_c_n":
                case "sync_c_n":
                case "buki1_c_n":
                case "buki2_c_n":
                case "ketu_c_n":
                    return "Pelvis";

                case "asi1_r_n":
                    return "RightThigh";
                case "asi2_r_n":
                    return "RightKnee";
                case "asi3_r_n":
                    return "RightFoot";
                case "asi4_r_n":
                    return "RightToe";

                case "asi1_l_n":
                    return "LeftThigh";
                case "asi2_l_n":
                    return "LeftKnee";
                case "asi3_l_n":
                    return "LeftFoot";
                case "asi4_l_n":
                    return "LeftToe";

                case "kosi_c_n":
                    return "Hips";
                case "mune_c_n":
                    return "Chest";

                case "kubi_c_n":
                    return "Neck";
                case "face_c_n":
                    return "Head";

                case "kata_l_n":
                    return "LeftCollar";
                case "ude1_l_n":
                    return "LeftShoulder";
                case "ude2_l_n":
                    return "LeftElbow";
                case "ude3_l_n":
                    return "LeftHand";

                case "kata_r_n":
                    return "RightCollar";
                case "ude1_r_n":
                    return "RightShoulder";
                case "ude2_r_n":
                    return "RightElbow";
                case "ude3_r_n":
                    return "RightHand";

                case "hito1_l_n":
                    return "LeftIndex_01";
                case "hito2_l_n":
                    return "LeftIndex_02";
                case "hito3_l_n":
                    return "LeftIndex_03";
                case "koyu1_l_n":
                    return "LeftLittle_01";
                case "koyu2_l_n":
                    return "LeftLittle_02";
                case "koyu3_l_n":
                    return "LeftLittle_03";
                case "naka1_l_n":
                    return "LeftMiddle_01";
                case "naka2_l_n":
                    return "LeftMiddle_02";
                case "naka3_l_n":
                    return "LeftMiddle_03";
                case "kusu1_l_n":
                    return "LeftRing_01";
                case "kusu2_l_n":
                    return "LeftRing_02";
                case "kusu3_l_n":
                    return "LeftRing_03";
                case "oya2_l_n":
                    return "LeftThumb_01";
                case "oya3_l_n":
                    return "LeftThumb_02";

                case "hito1_r_n":
                    return "RightIndex_01";
                case "hito2_r_n":
                    return "RightIndex_02";
                case "hito3_r_n":
                    return "RightIndex_03";
                case "koyu1_r_n":
                    return "RightLittle_01";
                case "koyu2_r_n":
                    return "RightLittle_02";
                case "koyu3_r_n":
                    return "RightLittle_03";
                case "naka1_r_n":
                    return "RightMiddle_01";
                case "naka2_r_n":
                    return "RightMiddle_02";
                case "naka3_r_n":
                    return "RightMiddle_03";
                case "kusu1_r_n":
                    return "RightRing_01";
                case "kusu2_r_n":
                    return "RightRing_02";
                case "kusu3_r_n":
                    return "RightRing_03";
                case "oya2_r_n":
                    return "RightThumb_01";
                case "oya3_r_n":
                    return "RightThumb_02";
                default:
                    index = parent;
                    continue;
            }
        }
    }

    public static string MapBipedSkeleton(List<(string Bone, int Parent)> joints, int index)
    {
        while (true)
        {
            if (index == -1) return string.Empty;
            (string? name, int parent) = joints[index];
            
            switch (name)
            {
                case "Dummy01":
                case "Bip01":
                case "Bip01 Footsteps":
                case "Bip01 Pelvis":
                case "Bip01 Spine":
                    return "Pelvis";
                case "Bip01 Spine1":
                    return "Chest";
                case "Bip01 Neck":
                    return "Neck";

                case "Bip01 L Clavicle":
                    return "LeftCollar";
                case "Bip01 L UpperArm":
                    return "LeftShoulder";
                case "Bip01 L Forearm":
                    return "LeftElbow";
                case "Bip01 L Hand":
                    return "LeftHand";
                case "Bip01 L Finger0":
                    return "LeftThumb_01";
                case "Bip01 L Finger1":
                    return "LeftIndex_01";
                case "Bip01 L Finger2":
                    return "LeftMiddle_01";
                case "Bip01 L Finger3":
                    return "LeftIndex_01";
                case "Bip01 L Finger4":
                    return "LeftLittle_01";

                case "Bip01 R Clavicle":
                    return "RightCollar";
                case "Bip01 R UpperArm":
                    return "RightShoulder";
                case "Bip01 R Forearm":
                    return "RightElbow";
                case "Bip01 R Hand":
                    return "RightHand";
                case "Bip01 R Finger0":
                    return "RightThumb_01";
                case "Bip01 R Finger1":
                    return "RightIndex_01";
                case "Bip01 R Finger2":
                    return "RightMiddle_01";
                case "Bip01 R Finger3":
                    return "RightIndex_01";
                case "Bip01 R Finger4":
                    return "RightLittle_01";

                case "Bip01 L Thigh":
                    return "LeftThigh";
                case "Bip01 L Calf":
                    return "LeftKnee";
                case "Bip01 L Foot":
                    return "LeftFoot";
                // case "Bip01 L Toe0": return "LeftToe";

                case "Bip01 R Thigh":
                    return "RightThigh";
                case "Bip01 R Calf":
                    return "RightKnee";
                case "Bip01 R Foot":
                    return "RightFoot";
                // case "Bip01 R Toe0": return "RightToe";   

                default:
                    index = parent;
                    continue;
            }
        }
    }
}