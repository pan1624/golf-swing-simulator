import cv2
import socket
import os
import importlib
import mediapipe as mp
import time

# 初始化 MediaPipe Pose 模块
mp_pose = mp.solutions.pose
pose = mp_pose.Pose(min_detection_confidence=0.5, min_tracking_confidence=0.5)
mp_drawing = mp.solutions.drawing_utils

# ✅ Socket 設置（目前已註解，如需啟用請取消註解）
host = '127.0.0.1'
port = 6001  # 確保端口號與 Unity 一致


try:
    client = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    client.connect((host, port))
    print("Connected to Unity server.")
except ConnectionError as e:
    print(f"Failed to connect to Unity server: {e}")
    exit()

# ✅ 定義要傳送的關鍵點索引
important_landmarks = [9, 9, 0, 12, 14, 16, 18, 11, 13, 15, 17, 23, 24, 25, 26, 29, 30]

# ✅ 嘗試開啟影片（修正路徑格式）
video_path = r"D:\college hw\專題\修正slow swing.mp4"
cap = cv2.VideoCapture(0)

if not cap.isOpened():
    raise FileNotFoundError(f"❌ 無法開啟影片: {video_path}")

print("✅ 成功開啟影片！")

#黃色圓心：換相機
#綠色圓心：開始遊戲
#灰色圓心：確認下一杆
#紫色圓心：向左
#藍色圓心：向右


circle_radius = 50
circle_center = (500, 200)  # 遊戲開始的圓心
circle_center1 = (165, 50)  # 換相機的圓心
circle_center2 = (50, 400)  # 方向往左的圓心
circle_center3 = (50, 200)  # 方向往右的圓心
circle_center4 = (50, 50)  # 下一杆的灰色圓心

# 監控的關鍵點索引
monitored_landmark_idx = 18  # 這裡假設監控第18個關鍵點 右手

# 記錄關鍵點在開始遊戲圓內的時間
start_time = None
show_text = False
show_time = None
inside_time = 0

# 記錄關鍵點在換相機圓內的時間
start_time1 = None
show_text1 = False
show_time1 = None
inside_time1 = 0

# 記錄關鍵點在方向往左圓內的時間
start_time2 = None
show_text2 = False
show_time2 = None
inside_time2 = 0

# 記錄關鍵點在方向往右圓內的時間
start_time3 = None
show_text3 = False
show_time3 = None
inside_time3 = 0

# 記錄關鍵點在灰色圓內的時間
start_time4 = None
show_text4 = False
show_time4 = None
inside_time4 = 0

while cap.isOpened():
    ret, frame = cap.read()
    if not ret:
        print("❌ 錯誤: 無法擷取畫面。")
        break

    # ✅ 轉換為 RGB（Mediapipe 需要此格式）
    image = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
    image.flags.writeable = False
    results = pose.process(image)

    # ✅ 傳送關鍵點資料到 Unity
    if results.pose_landmarks:
        data = ""
        for idx in important_landmarks:
            # ✅ 替換 `9` 為 `23` 和 `24` 的平均點
            if idx == 9:
                landmark_23 = results.pose_landmarks.landmark[23]
                landmark_24 = results.pose_landmarks.landmark[24]
                avg_x = (landmark_23.x + landmark_24.x) / 2
                avg_y = (landmark_23.y + landmark_24.y) / 2
                avg_visibility = (landmark_23.visibility + landmark_24.visibility) / 2
                keypoint_str = f"{avg_x:.4f},{avg_y:.4f},{avg_visibility:.4f}"
            else:
                landmark = results.pose_landmarks.landmark[idx]
                keypoint_str = f"{landmark.x:.4f},{landmark.y:.4f},{landmark.visibility:.4f}"

            print(f"傳送數據: {keypoint_str}")
            data += f"{keypoint_str}\n"


        
        try:
            client.sendall(data.encode('utf-8'))
            print("📡 數據成功發送到 Unity！")
        except ConnectionError as e:
            print(f"❌ 傳送錯誤: {e}")
            break
        

        # 檢查監控的關鍵點是否在開始遊戲圓內
        landmark = results.pose_landmarks.landmark[monitored_landmark_idx]
        landmark_x = int(landmark.x * frame.shape[1])
        landmark_y = int(landmark.y * frame.shape[0])
        distance = ((landmark_x - circle_center[0]) ** 2 + (landmark_y - circle_center[1]) ** 2) ** 0.5

        if distance <= circle_radius:
            if start_time is None:
                start_time = time.time()
            inside_time = time.time() - start_time
            if inside_time >= 2:
                show_text = True
                show_time = time.time()
                try:
                    client.sendall("start_game".encode('utf-8'))
                    print(" 發送 'start_game' 給 Unity！")
                except ConnectionError as e:
                    print(f"❌ 傳送錯誤: {e}")
                    break
        else:
            start_time = None
            inside_time = 0

        # 檢查監控的關鍵點是否在換相機圓內
        distance1 = ((landmark_x - circle_center1[0]) ** 2 + (landmark_y - circle_center1[1]) ** 2) ** 0.5

        if distance1 <= circle_radius:
            if start_time1 is None:
                start_time1 = time.time()
            inside_time1 = time.time() - start_time1
            if inside_time1 >= 2:
                show_text1 = True
                show_time1 = time.time()
                try:
                    client.sendall("camera_change".encode('utf-8'))
                    print(" 發送 'camera_change' 給 Unity！")
                except ConnectionError as e:
                    print(f"❌ 傳送錯誤: {e}")
                    break
        else:
            start_time1 = None
            inside_time1 = 0

        # 檢查監控的關鍵點是否在方向往左圓內
        distance2 = ((landmark_x - circle_center2[0]) ** 2 + (landmark_y - circle_center2[1]) ** 2) ** 0.5

        if distance2 <= circle_radius:
            if start_time2 is None:
                start_time2 = time.time()
            inside_time2 = time.time() - start_time2
            if inside_time2 >= 1:
                show_text2 = True
                show_time2 = time.time()
                try:
                    client.sendall("move_left".encode('utf-8'))
                    print(" 發送 'move_left' 給 Unity！")
                except ConnectionError as e:
                    print(f"❌ 傳送錯誤: {e}")
                    break
        else:
            start_time2 = None
            inside_time2 = 0

        # 檢查監控的關鍵點是否在方向往右圓內
        distance3 = ((landmark_x - circle_center3[0]) ** 2 + (landmark_y - circle_center3[1]) ** 2) ** 0.5

        if distance3 <= circle_radius:
            if start_time3 is None:
                start_time3 = time.time()
            inside_time3 = time.time() - start_time3
            if inside_time3 >= 1:
                show_text3 = True
                show_time3 = time.time()
                try:
                    client.sendall("move_right".encode('utf-8'))
                    print(" 發送 'move_right' 給 Unity！")
                except ConnectionError as e:
                    print(f"❌ 傳送錯誤: {e}")
                    break
        else:
            start_time3 = None
            inside_time3 = 0

        # 檢查監控的關鍵點是否在灰色圓內
        distance4 = ((landmark_x - circle_center4[0]) ** 2 + (landmark_y - circle_center4[1]) ** 2) ** 0.5

        if distance4 <= circle_radius:
            if start_time4 is None:
                start_time4 = time.time()
            inside_time4 = time.time() - start_time4
            if inside_time4 >= 1:
                show_text4 = True
                show_time4 = time.time()
                try:
                    client.sendall("next".encode('utf-8'))
                    print(" 發送 'next' 給 Unity！")
                except ConnectionError as e:
                    print(f"❌ 傳送錯誤: {e}")
                    break
        else:
            start_time4 = None
            inside_time4 = 0

    # ✅ 繪製骨架
    image.flags.writeable = True
    image = cv2.cvtColor(image, cv2.COLOR_RGB2BGR)
    mp_drawing.draw_landmarks(image, results.pose_landmarks, mp_pose.POSE_CONNECTIONS)

    # 將監控的關鍵點顯示為藍色
    if results.pose_landmarks:
        landmark = results.pose_landmarks.landmark[monitored_landmark_idx]
        landmark_x = int(landmark.x * frame.shape[1])
        landmark_y = int(landmark.y * frame.shape[0])
        cv2.circle(image, (landmark_x, landmark_y), 5, (255, 0, 0), -1)  # 藍色

    # 創建透明圖層
    overlay = image.copy()
    output = image.copy()

    # 在透明圖層上畫圓宣告擊球開始
    x, y = circle_center
    radius = circle_radius
    color = (0, 255, 0)  # 綠色
    thickness = -1  # 填充圓形
    cv2.circle(overlay, (x, y), radius, color, thickness)

    # 在透明圖層上畫圓宣告換相機
    x1, y1 = circle_center1
    radius = circle_radius
    color = (0, 255, 255)  # 黃色
    thickness = -1  # 填充圓形
    cv2.circle(overlay, (x1, y1), radius, color, thickness)

    # 在透明圖層上畫圓宣告方向往左
    x2, y2 = circle_center2
    radius = circle_radius
    color = (255, 0, 0)  # 藍色
    thickness = -1  # 填充圓形
    cv2.circle(overlay, (x2, y2), radius, color, thickness)

    # 在透明圖層上畫圓宣告方向往右
    x3, y3 = circle_center3
    radius = circle_radius
    color = (255, 255, 0)  # 黃色
    thickness = -1  # 填充圓形
    cv2.circle(overlay, (x3, y3), radius, color, thickness)

    # 在透明圖層上畫灰色圓
    x4, y4 = circle_center4
    radius = circle_radius
    color = (64, 64, 64)  # 灰色
    thickness = -1  # 填充圓形
    cv2.circle(overlay, (x4, y4), radius, color, thickness)

    # 混合透明圖層與原始圖像
    alpha = 0.2  # 透明度
    cv2.addWeighted(overlay, alpha, output, 1 - alpha, 0, output)

    # 在 output 上顯示文字
    if show_text:
        cv2.putText(output, "start!", (20, 15), cv2.FONT_HERSHEY_TRIPLEX, 0.5, (0, 0, 255), 1)
        if show_time is None:
            show_time = time.time()
        elif time.time() - show_time >= 1:
            show_text = False
    else:
        cv2.putText(output, f"Inside: {inside_time:.1f}s", (20, 15), cv2.FONT_HERSHEY_TRIPLEX, 0.5, (0, 255, 0), 1)

    if show_text1:
        cv2.putText(output, "camera change!", (20, 30), cv2.FONT_HERSHEY_TRIPLEX, 0.5, (0, 0, 255), 1)
        if show_time1 is None:
            show_time1 = time.time()
        elif time.time() - show_time1 >= 1:
            show_text1 = False
    else:
        cv2.putText(output, f"Inside : {inside_time1:.1f}s", (20, 30), cv2.FONT_HERSHEY_TRIPLEX, 0.5, (0, 255, 0), 1)

    if show_text2:
        cv2.putText(output, "move left!", (500, 15), cv2.FONT_HERSHEY_TRIPLEX, 0.5, (0, 0, 255), 1)
        if show_time2 is None:
            show_time2 = time.time()
        elif time.time() - show_time2 >= 1:
            show_text2 = False
    else:
        cv2.putText(output, f"Inside Left: {inside_time2:.1f}s", (500, 15), cv2.FONT_HERSHEY_TRIPLEX, 0.5, (0, 255, 0), 1)

    if show_text3:
        cv2.putText(output, "move right!", (500, 30), cv2.FONT_HERSHEY_TRIPLEX, 0.5, (0, 0, 255), 1)
        if show_time3 is None:
            show_time3 = time.time()
        elif time.time() - show_time3 >= 1:
            show_text3 = False
    else:
        cv2.putText(output, f"Inside Right: {inside_time3:.1f}s", (500, 30), cv2.FONT_HERSHEY_TRIPLEX, 0.5, (0, 255, 0), 1)

    if show_text4:
        cv2.putText(output, "next!", (250, 15), cv2.FONT_HERSHEY_TRIPLEX, 0.5, (0, 0, 255), 1)
        if show_time4 is None:
            show_time4 = time.time()
        elif time.time() - show_time4 >= 1:
            show_text4 = False
    else:
        cv2.putText(output, f"Inside Gray: {inside_time4:.1f}s", (250, 15), cv2.FONT_HERSHEY_TRIPLEX, 0.5, (0, 255, 0), 1)

    cv2.imshow('MediaPipe Pose Detection', output)

    if cv2.waitKey(10) & 0xFF == ord('q'):
        break

cap.release()
client.close()  # 若啟用 socket，請取消註解
cv2.destroyAllWindows()


