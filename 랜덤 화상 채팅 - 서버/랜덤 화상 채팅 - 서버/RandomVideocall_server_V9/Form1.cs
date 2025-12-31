using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using System.Threading;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using System.Data;

namespace RandomVideocall_V2
{
    public partial class Form1 : Form
    {
        private System.Windows.Forms.Timer statsTimer;// 통계 정보를 주기적으로 업데이트하는 타이머 :2초마다 접속자 수, 대기 인원, 매칭 수 표시

        private TcpListener tcpListener; // TDP 서버 리스너 객체 ( 클라이언트 연결을 기다림)

        private bool isRunning = false;// 실행상태

        private Thread acceptThread; // 클라이언트 수락 스레드 (종료 시 대기하기 위해 추적)



        private List<string> waitQ = new List<string>(); // 매칭 IP 대기 리스트 
        private Dictionary<string, string> IpMatch = new Dictionary<string, string>(); // key 내 IP주소 : value 상대IP


        private object lockObj = new object(); // 스레드 동기화를 위한  잠금 객체 

        private string connectionDB = "Server=155.230.235.248;Port=32065;Database=gaaaa;Uid=kgygemini01;Pwd=kgy030604;";


        private Dictionary<string, TcpClient> activeClient = new Dictionary<string, TcpClient>(); // 연결된 클라이언트 목록 

        private Dictionary<string, NetworkStream> clientStream = new Dictionary<string, NetworkStream>(); // 상대방 ip : 상대방 네트워크 스트림 


        private Dictionary<string, string> clientNames = new Dictionary<string, string>(); // 사용자 ip : 사용지 이름 

        private Dictionary<string, int> clientUDPPorts = new Dictionary<string, int>(); // 상대 ip : 상대 UDP 포트 번호 (P2P)

        // UDP 릴레이 관련 변수
        private UdpClient udpRelay; // UDP 릴레이 서버
        private const int UDP_RELAY_PORT = 9000; // 릴레이 포트 번호
        private Dictionary<string, IPEndPoint> clientUdpEndPoints = new Dictionary<string, IPEndPoint>(); // 클라이언트 IP : UDP 엔드포인트

        private Size buttonDefaultSize;
        private Font buttonDefaultFont;
        private Color buttonDefaultBackColor;

        private int _inputRadius = 8;
        private int _panelRadius = 12;
        private Color _borderColor = Color.FromArgb(220, 190, 190, 190);
        private float _borderWidth = 1f;

        private Color _customBorderColor = Color.FromArgb(200, 150, 150, 150); // 원하는 회색으로 조정
        private int _customBorderThickness = 2; // 1 또는 2 등

        public Form1()
        {

            InitializeComponent();
            ApplyRoundedToControls();

            // 컨트롤이 이동/크기 변경되면 폼을 다시 그리도록 연결
            if (this.textBox1 != null)
            {
                this.textBox1.LocationChanged -= (s, e) => this.Invalidate();
                this.textBox1.SizeChanged -= (s, e) => this.Invalidate();
                this.textBox1.LocationChanged += (s, e) => this.Invalidate();
                this.textBox1.SizeChanged += (s, e) => this.Invalidate();
            }
            if (this.textBox2 != null)
            {
                this.textBox2.LocationChanged -= (s, e) => this.Invalidate();
                this.textBox2.SizeChanged -= (s, e) => this.Invalidate();
                this.textBox2.LocationChanged += (s, e) => this.Invalidate();
                this.textBox2.SizeChanged += (s, e) => this.Invalidate();
            }

            textBox1.Text = "155.230.235.221";  // 서버 IP 주소 
            textBox2.Text = "8080";              // 서버 TCP 포트 번호

            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            this.DoubleBuffered = true;

            buttonDefaultSize = button1.Size;
            buttonDefaultFont = button1.Font;
            buttonDefaultBackColor = button1.BackColor;

            panel1.AutoScroll = true;
            panel1.BackColor = Color.White;


            if (this.Controls["labelConnectedUsers"] == null)// 중복 방지 ( 이름이 없을때만 폼에 표시)
            {
                Label labelConnectedUsers = new Label();
                labelConnectedUsers.Name = "labelConnectedUsers";
                labelConnectedUsers.AutoSize = true;
                labelConnectedUsers.Font = new Font("맑은 고딕", 10, FontStyle.Bold);
                labelConnectedUsers.Location = new Point(20, 100);
                this.Controls.Add(labelConnectedUsers);
            }

            AddLog("서버 준비 완료", Color.Blue);
            AddLog("서버 시작 버튼을 눌러주세요", Color.Black);
        }

        private GraphicsPath RoundedRect(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            if (rect.Width < d || rect.Height < d)
            {
                path.AddRectangle(rect);
                path.CloseFigure();
                return path;
            }

            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        // 초기 적용 호출(생성자에서 InitializeComponent() 다음에 호출)
        private void ApplyRoundedToControls()
        {
            //textBox1, textBox2에 대해 wrapper panel 생성 및 적용
            WrapControlInRoundedPanel(textBox1, _inputRadius);
            WrapControlInRoundedPanel(textBox2, _inputRadius);

        }

        private void WrapControlInRoundedPanel(Control child, int radius)
        {
            if (child == null) return;
            var parent = child.Parent;
            if (parent == null) return;

            // 이미 wrapper로 감싸졌는지 확인
            if (parent is Panel pParent && pParent.Tag is string t && t == "RoundedWrapperFor_" + child.Name)
            {
                pParent.Resize -= Wrapper_ResizeSetRegion;
                pParent.Resize += Wrapper_ResizeSetRegion;
                pParent.Paint -= Wrapper_PaintBorder;
                pParent.Paint += Wrapper_PaintBorder;
                return;
            }

            // 새 wrapper 생성
            var wrapper = new Panel
            {
                Name = "wrap_" + child.Name,
                BackColor = child.BackColor,
                Margin = child.Margin,
                Padding = new Padding(6, 4, 6, 4),
                Dock = DockStyle.Fill, // 반드시 셀을 채우도록
                Anchor = child.Anchor,
                TabIndex = child.TabIndex,
                Tag = "RoundedWrapperFor_" + child.Name
            };

            // 부모가 TableLayoutPanel인지, 그리고 child가 직접 그 TLP의 자식인지 확인
            var tlp = parent as TableLayoutPanel;
            if (tlp != null && tlp.Controls.Contains(child))
            {
                var pos = tlp.GetPositionFromControl(child);
                // 보존해야 할 속성들(Span)
                int colSpan = tlp.GetColumnSpan(child);
                int rowSpan = tlp.GetRowSpan(child);

                // remove child, add wrapper at same cell, then add child into wrapper
                int controlIndex = tlp.Controls.GetChildIndex(child);
                tlp.Controls.Remove(child);

                // Add wrapper at same cell and set spans
                tlp.Controls.Add(wrapper, pos.Column, pos.Row);
                if (colSpan > 1) tlp.SetColumnSpan(wrapper, colSpan);
                if (rowSpan > 1) tlp.SetRowSpan(wrapper, rowSpan);

                // Ensure wrapper is at same z-order roughly
                tlp.Controls.SetChildIndex(wrapper, controlIndex);
            }
            else
            {
                // 부모가 TableLayoutPanel이 아니거나 child가 중첩 TLP 안에 더 깊이 있으면 일반 처리
                int idx = parent.Controls.GetChildIndex(child);
                parent.Controls.Remove(child);
                parent.Controls.Add(wrapper);
                parent.Controls.SetChildIndex(wrapper, idx);
            }

            // child를 wrapper에 넣기 전에 보존해둔 속성 복원
            child.Margin = new Padding(0);
            child.Dock = DockStyle.Fill;
            wrapper.Controls.Add(child);

            // 이벤트 등록: Region 설정(Resize)과 테두리(Paint)
            wrapper.Resize -= Wrapper_ResizeSetRegion;
            wrapper.Resize += Wrapper_ResizeSetRegion;
            wrapper.Paint -= Wrapper_PaintBorder;
            wrapper.Paint += Wrapper_PaintBorder;

            // 강제 초기화
            Wrapper_ResizeSetRegion(wrapper, EventArgs.Empty);
            wrapper.Invalidate();
        }


        // wrapper Resize: Region(클리핑) 생성 — 이렇게 하면 자식 컨트롤 모서리가 라운드로 잘림
        private void Wrapper_ResizeSetRegion(object sender, EventArgs e)
        {
            if (!(sender is Control wrapper)) return;
            int radius = _inputRadius;
            Rectangle r = wrapper.ClientRectangle;
            // inset 해줘서 테두리가 잘리지 않도록 여유 확보
            var inner = Rectangle.Inflate(r, -(int)Math.Ceiling(_borderWidth), -(int)Math.Ceiling(_borderWidth));
            if (inner.Width <= 0 || inner.Height <= 0) return;

            using (var path = RoundedRect(inner, radius))
            {
                // 새 Region 할당 (기존 Region은 GC가 처리)
                wrapper.Region = new Region(path);
            }
        }

        // wrapper Paint: 내부 배경 채우기 & 테두리 그리기
        private void Wrapper_PaintBorder(object sender, PaintEventArgs e)
        {
            if (!(sender is Control wrapper)) return;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // 0.5 픽셀 보정 -> 선명한 1px 테두리
            g.TranslateTransform(0.5f, 0.5f);

            var rect = wrapper.ClientRectangle;
            var inner = Rectangle.Inflate(rect, -(int)Math.Ceiling(_borderWidth), -(int)Math.Ceiling(_borderWidth));

            using (var path = RoundedRect(inner, _inputRadius))
            {
                // 배경 채우기 (wrapper.BackColor과 동일하게)
                using (var brush = new SolidBrush(wrapper.BackColor))
                    g.FillPath(brush, path);

                // 얇은 테두리
                using (var pen = new Pen(_borderColor, _borderWidth) { Alignment = PenAlignment.Inset })
                    g.DrawPath(pen, path);
            }

            g.ResetTransform();
        }


        // 시작할때 호출 
        private void StartServer()
        {
            try
            {
                int port = int.Parse(textBox2.Text);// 사용자 포트번호 -> int 

                tcpListener = new TcpListener(IPAddress.Any, port);// 모든 네트워크에서 연결 수락
                tcpListener.Start();

                //UDP 릴레이 서버 시작
                udpRelay = new UdpClient(UDP_RELAY_PORT);
                udpRelay.BeginReceive(OnUdpRelayReceive, null);


                AddLog(" 서버 시작!", Color.Green);
                AddLog($" TCP 포트: {port}", Color.Green);
                AddLog($" UDP 릴레이 포트: {UDP_RELAY_PORT}", Color.Green);
                AddLog($" 시간: {DateTime.Now:yyyy-MM-dd HH:mm:ss}", Color.Green);

                button1.Text = "서버 중지";
                button1.BackColor = Color.OrangeRed;
                button1.Size = buttonDefaultSize;
                button1.Font = buttonDefaultFont;

                DBConnection(); // 연결 테스트 

                statsTimer = new System.Windows.Forms.Timer();// 통계 타이머 
                statsTimer.Interval = 2000; // 2초마다 실행


                statsTimer.Tick += UpdateStats;// 타이머 이벤트 핸들러 등록
                statsTimer.Start();// 타이머 시작


                // 스레드 이용 => 지속적으로 이용자 수락
                acceptThread = new Thread(AcceptClients);
                acceptThread.IsBackground = true;  // 백그라운드 스레드로 설정 (메인 스레드 종료 시 자동 종료)
                acceptThread.Start();

                isRunning = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("서버 시작 실패: " + ex.Message);
                AddLog($"오류: {ex.Message}\n{ex.StackTrace}", Color.Red);
                StopServer();
            }
        }

        // UDP 릴레이 수신 메서드
        private void OnUdpRelayReceive(IAsyncResult ar)
        {
            try
            {
                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = udpRelay.EndReceive(ar, ref remoteEP);

                // 다음 수신 대기
                if (isRunning && udpRelay != null)
                {
                    udpRelay.BeginReceive(OnUdpRelayReceive, null);
                }

                // 송신자 IP 확인
                string senderIP = remoteEP.Address.ToString();

                // 클라이언트의 UDP 엔드포인트 저장/업데이트
                lock (lockObj)
                {
                    clientUdpEndPoints[senderIP] = remoteEP;
                }

                // 매칭된 상대방에게 패킷 전달
                lock (lockObj)
                {
                    if (IpMatch.ContainsKey(senderIP))
                    {
                        string partnerIP = IpMatch[senderIP];

                        // 상대방의 UDP 엔드포인트가 있으면 전달
                        if (clientUdpEndPoints.ContainsKey(partnerIP))
                        {
                            IPEndPoint partnerEP = clientUdpEndPoints[partnerIP];
                            udpRelay.Send(data, data.Length, partnerEP);

                            // 디버그 로그 (선택사항)
                            // AddLog($"UDP 릴레이: {senderIP} → {partnerIP} ({data.Length} bytes)", Color.Gray);
                        }
                    }
                }
            }
            catch (ObjectDisposedException){ }
            catch (Exception ex)
            {
                AddLog($"UDP 릴레이 오류: {ex.Message}", Color.Red);

                try
                {// 에러 발생 시 다시 수신 대기
                    if (isRunning && udpRelay != null)
                    {
                        udpRelay.BeginReceive(OnUdpRelayReceive, null);
                    }
                }
                catch { }
            }
        }

        // 중단할때 호출
        private void StopServer()
        {
            isRunning = false;

            if (tcpListener != null){

                try{
                    tcpListener.Stop();
                }
                catch (Exception ex)
                {
                    AddLog($"TCP  중지 오류: {ex.Message}", Color.Red);
                }
            }

            if (statsTimer != null)
            {
                statsTimer.Stop();
            }

         
            if (udpRelay != null)
            {
                try {
                    udpRelay.Close();
                    udpRelay.Dispose();
                    udpRelay = null;
                }
                catch (Exception ex)
                {
                    AddLog($"UDP 릴레이 중지 오류: {ex.Message}", Color.Red);
                }
            }

            lock (lockObj)// lock을 사용하여 다른 스레드가 동시에 접근하지 못하도록 보호
            {
                // 모든 활성 클라이언트 연결 종료
                foreach (var client in activeClient.Values)
                {
                    if (client != null)
                    {
                        try
                        {
                            client.Close();  // TcpClient 연결 종료
                        }
                        catch { }
                    }
                }
                // 모든 딕셔너리랑 리스트 초기화
                activeClient.Clear();
                clientStream.Clear();
                clientNames.Clear();
                clientUDPPorts.Clear();
                clientUdpEndPoints.Clear(); // UDP 엔드포인트 딕셔너리 초기화
                waitQ.Clear();
                IpMatch.Clear();
            }

           
            if (acceptThread != null && acceptThread.IsAlive)
            {
                try
                {
                    // 스레드가 종료될 때까지 최대 3초 대기
                    if (!acceptThread.Join(3000))
                    {
                        AddLog("스레드가 3초 내에 종료되지 않음", Color.Orange);
                    }
                    else
                    {
                        AddLog(" 스레드 정상 종료", Color.Green);
                    }
                }
                catch (Exception ex)
                {
                    AddLog($"스레드 종료 대기 오류: {ex.Message}", Color.Red);
                }
            }

            AddLog(" 서버 중지", Color.Red);
            AddLog($" 시간: {DateTime.Now:yyyy-MM-dd HH:mm:ss}", Color.Red);


            button1.Text = "서버 시작";                   // 버튼 텍스트 변경
            button1.BackColor = buttonDefaultBackColor;
            button1.Size = buttonDefaultSize;
            button1.Font = buttonDefaultFont;             // 포트 입력 활성화
        }

        // DE연결
        private void DBConnection()
        {
            try
            {

                using (MySqlConnection conn = new MySqlConnection(connectionDB)) // using 문을 사용하면  자동 리소스 해제
                {

                    conn.Open();  // 연결 실패 시 예외 발생
                    AddLog(" DB 연결 성공", Color.Green);

                    // ⭐ block 테이블 구조 확인
                    try
                    {
                        string describeQuery = "DESCRIBE block";
                        MySqlCommand descCmd = new MySqlCommand(describeQuery, conn);
                        using (MySqlDataReader reader = descCmd.ExecuteReader())
                        {
                            AddLog(" block 테이블 구조:", Color.Cyan);
                            while (reader.Read())
                            {
                                string field = reader["Field"].ToString();
                                string type = reader["Type"].ToString();
                                string nullVal = reader["Null"].ToString();
                                string key = reader["Key"].ToString();
                                AddLog($"    - {field} ({type}) NULL={nullVal} KEY={key}", Color.Cyan);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        AddLog($" block 테이블이 없음: {ex.Message}", Color.Yellow);

                        // 중단을 위한 block 테이블 생성
                        string createTableQuery = @"
                            CREATE TABLE IF NOT EXISTS block (
                                blocker_ip VARCHAR(15) NOT NULL,
                                blocked_ip VARCHAR(15) NOT NULL,
                                is_block VARCHAR(1) DEFAULT '1',
                                PRIMARY KEY (blocker_ip, blocked_ip, is_block)
                            )";
                        MySqlCommand createCmd = new MySqlCommand(createTableQuery, conn);
                        createCmd.ExecuteNonQuery();
                        AddLog(" block 테이블 생성 완료", Color.Green);
                    }

                    // 사용자 수 세기
                    string query = "SELECT COUNT(*) FROM user";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    int userCount = Convert.ToInt32(cmd.ExecuteScalar());
                    AddLog($"  등록 사용자: {userCount}명", Color.Gray);

                    // ⭐ 차단 레코드 수 확인
                    try
                    {
                        string blockQuery = "SELECT COUNT(*) FROM block";
                        MySqlCommand blockCmd = new MySqlCommand(blockQuery, conn);
                        int blockCount = Convert.ToInt32(blockCmd.ExecuteScalar());
                        AddLog($"  전체 차단 레코드: {blockCount}개", Color.Gray);
                    }
                    catch
                    {
                        AddLog($"  차단 레코드 조회 실패", Color.Yellow);
                    }
                }
            }
            catch (Exception ex)
            {
                AddLog($"DB 연결 실패: {ex.Message}", Color.Red);
            }
        }

        // 2초마다 호출 
        private void UpdateStats(object sender, EventArgs e)
        {
            if (!isRunning) return;

            lock (lockObj)
            {
                // activeMatches는 양방향으로 저장되므로 나누기 2를 해야 실제 매칭 수기ㅏ 됨.
                AddLog($" [접속자: {activeClient.Count}명 | 대기: {waitQ.Count}명 | 매칭: {IpMatch.Count / 2}개]", Color.DarkBlue);
            }
        }

        // Panel에 로그 추가 함수
        private void AddLog(string message, Color color)
        {
            if (panel1.InvokeRequired)
            {
                panel1.Invoke(new Action(() => AddLog(message, color)));
                return;
            }

            Label log = new Label();
            log.Text = $"[{DateTime.Now:HH:mm:ss}] {message}";
            log.ForeColor = color;
            log.AutoSize = true;
            log.Font = new Font("맑은 고딕", 9);
            log.Padding = new Padding(5, 2, 5, 2);

            int y = 5;
            if (panel1.Controls.Count > 0)
            {
                // 가장 마지막 라벨을 찾아서 그 아래에 위치시킴
                Control last = panel1.Controls[panel1.Controls.Count - 1];
                y = last.Bottom + 2;
            }
            log.Location = new Point(5, y);

            panel1.Controls.Add(log);
            panel1.ScrollControlIntoView(log);

            if (panel1.Controls.Count > 100)
            {
                panel1.Controls.RemoveAt(0);
            }
        }

        // 연결 수락 함수
        private void AcceptClients()
        {
            AddLog("클라이언트 대기 중...", Color.Blue);
            while (isRunning)
            {
                try{
                    TcpClient client = tcpListener.AcceptTcpClient();// 클라이언트 정보 수락 대기

                    //여러명의 서버 직원이 있고 손님이 오면 각 직원이 응대하러 간다고 생각하면됨.
                    Thread clientThread = new Thread(() => HandleClient(client)); // HandleClient() : 클라이언트 정보 받아오느 함수
                    clientThread.IsBackground = true;
                    clientThread.Start();
                }
                catch (Exception ex)
                {
                    // 서버가 중지되는 중이면 정상적인 종료이므로 오류 로그 출력 안 함
                    if (isRunning)
                    {
                        AddLog("클라이언트 수락 오류: " + ex.Message, Color.Red);
                    }
                    break; // 오류 발생 시 루프 종료 (continue 대신 break)
                }
            }// 스레드 종료 (UI 스레드에서 Join 대기 중일 수 있으므로 로그 출력 안 함)

        }

        private bool IsConnecting(string clientIP) // 클라이언트 연결 상태 확인 함수
        {
            lock (lockObj)
            {
                if (!activeClient.ContainsKey(clientIP))// 등록된 클라이언트인지 확인
                    return false;

                TcpClient client = activeClient[clientIP];

                try
                {

                    if (client == null || !client.Connected)
                        return false;

                    Socket socket = client.Client;

                    bool part1 = socket.Poll(1000, SelectMode.SelectRead);//소켓이 읽기 가능한 상태인지 확인
                    bool part2 = (socket.Available == 0);// 데이터가 있는 없는지  t: 없음 
                                                         //연결이 끊긴 상태
                    if (part1 && part2)//연결이 끊긴 상태
                        return false;

                    return true; //정상 연결
                }
                catch// 예외는 끊김으로 간주 
                {
                    return false;
                }
            }
        }


        private void RmoveWaitiQ()// 대기열에서 연결이 끊긴 클라이언트를 제거하는 함수 
        {
            lock (lockObj)
            {

                // 뒤에서부터 순회해야 삭제 시 인덱스 오류 방지
                for (int i = waitQ.Count - 1; i >= 0; i--)
                {
                    string ip = waitQ[i];

                    if (!IsConnecting(ip))  //연결이 끊긴 클라이언트를 찾아서 제거
                    {
                        waitQ.RemoveAt(i);
                    }
                }
            }
        }



        private void HandleClient(TcpClient client)// 클라이언트 정보 처리 함수
        {
            NetworkStream stream = null;  // 파이프
            string clientIP = null;

            try{

                // EndPoint: 연결된 상대방의 네트워크 주소 전반을 가져옴 
                EndPoint remoteEndPoint = client.Client.RemoteEndPoint;

                // IPEndPoint: 네트워크 주소에서 IP주소와 포트번호만 분리
                IPEndPoint ipEndPoint = (IPEndPoint)remoteEndPoint;

                clientIP = ipEndPoint.Address.ToString();
                stream = client.GetStream();


                lock (lockObj)
                {
                    activeClient[clientIP] = client;// 서버에 연결된 클라이언트 정보로 저장.
                    clientStream[clientIP] = stream;
                }

                AddLog($"클라이언트 연결: {clientIP}", Color.Green);


                byte[] buffer = new byte[65536];  // ㅋㅋ용량 최대

                while (isRunning && client.Connected)
                {

                    int bytes = stream.Read(buffer, 0, buffer.Length);

                    if (bytes == 0) break;

                    string message = Encoding.UTF8.GetString(buffer, 0, bytes).Trim();

                    AddLog($"← [{clientIP}] {message}", Color.Cyan);


                    string response = "";

                    if (message.StartsWith("REGISTER:")) //이름: 메세지
                    {

                        string[] parts = message.Substring(9).Split(':');
                        string userName = parts[0];

                        // UDP 포트가 제공된 경우 파싱, 없으면 0
                        int udpPort = parts.Length > 1 ? int.Parse(parts[1]) : 0;

                        // 등록 처리 함수 호출
                        response = ProcessRegister(clientIP, userName, udpPort);

                        // 클라이언트 정보 저장
                        lock (lockObj)
                        {
                            clientNames[clientIP] = userName;
                            clientUDPPorts[clientIP] = udpPort;
                        }


                        UpdateUsersList();
                    }
                   
                    else if (message == "MATCH")
                    {
                        response = ProcessMatch(clientIP);
                    }
                    
                    else if (message == "END")
                    {
                        ProcessEnd(clientIP);
                        response = "OK";
                    }
                    
                    else if (message.StartsWith("BLOCK:"))
                    {
                        AddLog($"BLOCK 명령 수신: {message} (from {clientIP})", Color.Magenta);
                        string blockedIP = message.Substring(6);  // 차단할 IP 추출
                        AddLog($" 차단할 IP 추출: {blockedIP}", Color.Magenta);
                        response = ProcessBlock(clientIP, blockedIP);
                        AddLog($" ProcessBlock 결과: {response}", Color.Magenta);
                    }
                    
                    else if (message.StartsWith("MSG:"))
                    {
                        string chatMessage = message.Substring(4);  // 메시지 내용 추출
                        ProcessMessage(clientIP, chatMessage);
                        response = "OK";
                    }
                    
                    else if (message == "HEARTBEAT")
                    {
                        response = "OK";  // 단순히 응답만 보냄
                    }
                    
                    else
                    {
                        response = "ERROR:Unknown command";
                    }

                    if (!string.IsNullOrEmpty(response))
                    {
                        
                        byte[] responseData = Encoding.UTF8.GetBytes(response);// 문자열을 UTF-8 바이트 배열로 변환

                        stream.Write(responseData, 0, responseData.Length); // 네트워크 스트림을 통해 전송

                        AddLog($"→ [{clientIP}] {response}", Color.Blue);// 전송한 응답 로그 출력
                    }
                }
            }
            catch (Exception ex)
            {
                
                AddLog($" [{clientIP}] 오류: {ex.Message}", Color.Red);
            }
            finally
            {
                
                if (clientIP != null)
                {
                    lock (lockObj)
                    {
                        
                        activeClient.Remove(clientIP);
                        clientStream.Remove(clientIP);
                        clientNames.Remove(clientIP);
                        clientUDPPorts.Remove(clientIP);
                        waitQ.Remove(clientIP);

                        
                        if (IpMatch.ContainsKey(clientIP))// 연결 종료 알림. 
                        {
                            string partner = IpMatch[clientIP];  // 매칭된 상대방 IP

                            // 매칭 정보 제거 (양방향 모두 제거)
                            IpMatch.Remove(clientIP);
                            IpMatch.Remove(partner);

                            // 상대방에게 연결 종료 알림
                            NotifyPartner(partner, "PARTNER_ENDED");
                        }
                    }

                    AddLog($" 연결 해제: {clientIP}", Color.Gray);
                    UpdateUsersList();  // UI 업데이트
                }

                
                stream?.Close();   // 네트워크 스트림 닫기
                client?.Close();   // TCP 클라이언트 닫기
            }
        }

        
        private void UpdateUsersList()
        {
            try{  
                if (this.InvokeRequired){
                   
                    if (this.IsDisposed) return; // 폼이 닫히는 중이면 업데이트 중단

                    this.Invoke(new Action(UpdateUsersList));// UI 스레드에서 다시 호출
                    return;
                }

                if (this.IsDisposed) return;  // UI 스레드에서도 폼 상태 재확인


                Label label = this.Controls["labelConnectedUsers"] as Label;

              
                if (label == null || label.IsDisposed) return;  // 레이블이 없거나 닫혔으면 중단


                lock (lockObj)
                {
                    // 접속자가 없는 경우
                    if (activeClient.Count == 0)
                    {
                        label.Text = "접속자 IP: 없음";
                        label.ForeColor = Color.Gray;
                    }
                    // 접속자가 있는 경우
                    else
                    {
                        StringBuilder sb = new StringBuilder();

                        // 헤더 추가
                        sb.AppendLine($"━━━ 접속자 목록 ({activeClient.Count}명) ━━━");

                        // 각 클라이언트 정보 추가
                        foreach (var kvp in activeClient)
                        {
                            string ip = kvp.Key;

                            // 사용자 이름 가져오기 
                            string name = clientNames.ContainsKey(ip) ? clientNames[ip] : "미등록";

                            // 상태 결정
                            string status = "";

                            if (IpMatch.ContainsKey(ip))
                            {
                                status = " [매칭 중]";  // 통화 중
                            }
                            else if (waitQ.Contains(ip))
                            {
                                status = " [대기 중]";  
                            }
                            else
                            {
                                status = " [접속]";    
                            }

                            // 한 줄 추가
                            sb.AppendLine($"• {name} ({ip}){status}");
                        }

                        // 레이블에 텍스트 설정
                        label.Text = sb.ToString();
                        label.ForeColor = Color.DarkBlue;
                    }
                }
            }
            catch (Exception ex)
            {
                
                System.Diagnostics.Debug.WriteLine($"UpdateUsersList failed: {ex.Message}");
            }
        }

        
        private string ProcessRegister(string clientIP, string userName, int udpPort)
        {
            try{
                using (MySqlConnection conn = new MySqlConnection(connectionDB))
                {
                    
                    conn.Open();

                    string checkQuery = "SELECT COUNT(*) FROM user WHERE ip = @ip";
                    MySqlCommand checkCmd = new MySqlCommand(checkQuery, conn);

                    checkCmd.Parameters.AddWithValue("@ip", clientIP);

                    int count = Convert.ToInt32(checkCmd.ExecuteScalar());


                    // 신규 등록 
                    if (count == 0)
                    {
                        string insertQuery = "INSERT INTO user (ip, name) VALUES (@ip, @name)";
                        MySqlCommand insertCmd = new MySqlCommand(insertQuery, conn);
                        insertCmd.Parameters.AddWithValue("@ip", clientIP);
                        insertCmd.Parameters.AddWithValue("@name", userName);

                        // 쿼리 실행 
                        insertCmd.ExecuteNonQuery();

                        AddLog($" 신규 사용자 등록: {userName} ({clientIP}:{udpPort})", Color.Green);
                        return "WELCOME"; 
                    }
                   
                    else
                    {
                        string updateQuery = "UPDATE user SET name = @name WHERE ip = @ip";
                        MySqlCommand updateCmd = new MySqlCommand(updateQuery, conn);
                        updateCmd.Parameters.AddWithValue("@name", userName);
                        updateCmd.Parameters.AddWithValue("@ip", clientIP);

                        // 쿼리 실행
                        updateCmd.ExecuteNonQuery();

                        AddLog($" 사용자 재접속: {userName} ({clientIP}:{udpPort})", Color.Blue);
                        return "WELCOME_BACK";  
                    }
                }
            }
            catch (Exception ex){
                AddLog($" 등록 실패 [{clientIP}]: {ex.Message}", Color.Red);
                return "ERROR:Registration failed";
            }
        }

        
        private void NotifyPartner(string partnerIP, string message)
        {
            try{
                lock (lockObj)
                {
                    
                    if (clientStream.ContainsKey(partnerIP))
                    {
                        NetworkStream stream = clientStream[partnerIP];

                      
                        byte[] data = Encoding.UTF8.GetBytes(message);
                        stream.Write(data, 0, data.Length);
                        AddLog($"→ [{partnerIP}] {message}", Color.Purple);
                    }
                }
            }
            catch (Exception ex){  
                AddLog($"알림 실패 [{partnerIP}]: {ex.Message}", Color.Red);
            }
        }

       
        private string ProcessMatch(string clientIP)
        {
            AddLog($"매칭 요청: [{clientIP}]", Color.Blue);
            RmoveWaitiQ();

            List<string> blockedIPs = GetBlockedIPs(clientIP);
            AddLog($" 차단 목록 조회 완료: {blockedIPs.Count}개 차단됨", Color.Gray);

            lock (lockObj)
            {
                
                if (IpMatch.ContainsKey(clientIP))
                {
                    
                    return "ALREADY_MATCHED:" + IpMatch[clientIP];
                }

                string partner = null;

                for (int i = 0; i < waitQ.Count; i++)
                {
                    string candidate = waitQ[i];

                    if (candidate == clientIP)// 자기 자신이면 안됨 
                    {
                        AddLog($"   [{candidate}] 자기 자신 - 건너뜀", Color.Gray);
                        continue;
                    }

                    if (!IsConnecting(candidate))// 연결 중이어야함. 
                    {
                        AddLog($"   [{candidate}] 연결 끊김 - 건너뜀", Color.Gray);
                        continue;
                    }

                  
                    if (blockedIPs.Contains(candidate)) // 내가 차단하지 않은 사람일걸 
                    {
                        AddLog($"   [{candidate}] 내가 차단한 사용자 - 건너뜀", Color.Orange);
                        continue;
                    }

                
                    if (IsBlocked(candidate, clientIP)) // 상대도 나를 차단 ㄴㄴ
                    {
                        AddLog($"   [{candidate}] 상대방이 나를 차단 - 건너뜀", Color.Orange);
                        continue;
                    }

                    // 든 조건 통과-> 매칭
                    partner = candidate;
                    waitQ.RemoveAt(i);
                    AddLog($" [{candidate}] 매칭 가능한 사용자 발견!", Color.Green);
                    break;
                }

                if (partner != null)
                {
                    

                    // 양방향 매칭 정보 저장
                    IpMatch[clientIP] = partner;
                    IpMatch[partner] = clientIP;

                    AddLog($" 매칭 성공: {clientIP} ↔ {partner}", Color.Green);

                    // 사용자 이름 가져오기
                    string clientName = clientNames.ContainsKey(clientIP) ? clientNames[clientIP] : "알 수 없음";
                    string partnerName = clientNames.ContainsKey(partner) ? clientNames[partner] : "알 수 없음";

                    // 서버 IP 가져오기 (Form의 textBox1에서)
                    string serverIP = "";
                    if (this.InvokeRequired)
                    {
                        this.Invoke(new Action(() => { serverIP = textBox1.Text; }));
                    }
                    else
                    {
                        serverIP = textBox1.Text;
                    }

                    //  상대방에게 매칭 정보 전송
                    // 형식: MATCHED:서버IP:상대이름:UDP릴레이포트:실제상대IP
                    NotifyPartner(partner, $"MATCHED:{serverIP}:{clientName}:{UDP_RELAY_PORT}:{clientIP}");// 클라이언트는 이제 상대방이 아닌 서버로 비디오 전송, 실제 상대IP는 차단용

                    //  요청자에게 매칭 정보 반환
                    return $"MATCHED:{serverIP}:{partnerName}:{UDP_RELAY_PORT}:{partner}";
                }
                else{ // 상대방 없을떄 

                    // 대기열에 추가 (중복 방지)
                    if (!waitQ.Contains(clientIP))
                    {
                        waitQ.Add(clientIP);
                        AddLog($"대기열 추가: {clientIP}", Color.Orange);
                    }

                    return "WAITING";  // 대기 중 상태 반환
                }
            }
        }

       
        private void ProcessEnd(string clientIP) // 통화 종료 
        {
            lock (lockObj)
            {
                
                waitQ.Remove(clientIP);// 대기 제거 

               
                if (IpMatch.ContainsKey(clientIP)) // 매칭 해제 
                {
                    // 매칭된 상대방 IP 가져오기
                    string partner = IpMatch[clientIP];

                    // 양방향 매칭 정보 제거
                    IpMatch.Remove(clientIP);
                    IpMatch.Remove(partner);

                    AddLog($" 통화 종료: {clientIP} ↔ {partner}", Color.Magenta);

                    // 상대방에게 종료 알림
                    NotifyPartner(partner, "PARTNER_ENDED");
                }
            }
        }

        
        private string ProcessBlock(string blockerIP, string blockedIP) // 차단 처리 함수 
        {
            AddLog($"━━━ 차단 요청 시작 ━━━", Color.Magenta);
            AddLog($"  차단자: {blockerIP}", Color.Magenta);
            AddLog($"  피차단자: {blockedIP}", Color.Magenta);

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionDB))
                {
                    
                    conn.Open();
                    AddLog($"   DB 연결 성공", Color.Green);

                   
                    string checkQuery = "SELECT COUNT(*) FROM block WHERE blocker_ip = @blocker AND blocked_ip = @blocked";
                    MySqlCommand checkCmd = new MySqlCommand(checkQuery, conn);
                    checkCmd.Parameters.AddWithValue("@blocker", blockerIP);
                    checkCmd.Parameters.AddWithValue("@blocked", blockedIP);
                    int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                    AddLog($" 기존 레코드 수: {count}개", Color.Gray);

                    
                    if (count == 0)
                    {
                        // 신규 차단: INSERT
                        AddLog($"  → 신규 차단 INSERT 실행...", Color.Yellow);
                        string insertQuery = "INSERT INTO block (blocker_ip, blocked_ip, is_block) VALUES (@blocker, @blocked, '1')";
                        AddLog($"쿼리: {insertQuery}", Color.Gray);
                        AddLog($"파라미터: @blocker={blockerIP}, @blocked={blockedIP}", Color.Gray);

                        MySqlCommand insertCmd = new MySqlCommand(insertQuery, conn);
                        insertCmd.Parameters.AddWithValue("@blocker", blockerIP);
                        insertCmd.Parameters.AddWithValue("@blocked", blockedIP);
                        int rowsAffected = insertCmd.ExecuteNonQuery();

                        AddLog($"INSERT 성공! 영향받은 행: {rowsAffected}", Color.Green);
                    }
                    else
                    {
                        // 기존 차단 재활성화: UPDATE
                        AddLog($" → 기존 차단 UPDATE 실행...", Color.Yellow);
                        string updateQuery = "UPDATE block SET is_block = '1' WHERE blocker_ip = @blocker AND blocked_ip = @blocked";
                        AddLog($"쿼리: {updateQuery}", Color.Gray);

                        MySqlCommand updateCmd = new MySqlCommand(updateQuery, conn);
                        updateCmd.Parameters.AddWithValue("@blocker", blockerIP);
                        updateCmd.Parameters.AddWithValue("@blocked", blockedIP);
                        int rowsAffected = updateCmd.ExecuteNonQuery();

                        AddLog($" UPDATE 성공! 영향받은 행: {rowsAffected}", Color.Green);
                    }

                    //저장 확인 쿼리
                    string verifyQuery = "SELECT is_block FROM block WHERE blocker_ip = @blocker AND blocked_ip = @blocked";
                    MySqlCommand verifyCmd = new MySqlCommand(verifyQuery, conn);
                    verifyCmd.Parameters.AddWithValue("@blocker", blockerIP);
                    verifyCmd.Parameters.AddWithValue("@blocked", blockedIP);
                    string savedValue = verifyCmd.ExecuteScalar()?.ToString();
                    AddLog($"   DB 저장 확인: is_block = '{savedValue}'", Color.Cyan);

                    AddLog($" 차단 등록 완료: {blockerIP} → {blockedIP}", Color.Red);

                    
                    lock (lockObj)
                    {
                        // 차단한 사람이 현재 차단당한 사람과 통화 중이면
                        if (IpMatch.ContainsKey(blockerIP) && IpMatch[blockerIP] == blockedIP)
                        {
                            AddLog($"  → 현재 통화 중이므로 즉시 종료", Color.Orange);
                            ProcessEnd(blockerIP);  // 통화 즉시 종료
                        }
                    }
                    AddLog($"━━━ 차단 요청 완료 ━━━", Color.Magenta);
                    return "BLOCKED_OK";
                }
            }
            catch (Exception ex)
            {
                AddLog($" 차단 실패 [{blockerIP}→{blockedIP}]", Color.Red);
                return "ERROR:Block failed";
            }
        }

 
        private void ProcessMessage(string senderIP, string message)
        {
            lock (lockObj)
            {
             
                if (IpMatch.ContainsKey(senderIP))
                {
                    
                    string partnerIP = IpMatch[senderIP];// 매칭된 상대방 IP 가져오기
                    string senderName = clientNames.ContainsKey(senderIP) ? clientNames[senderIP] : "알 수 없음"; // 발신자 이름 가져오기


                    NotifyPartner(partnerIP, $"MSG:{senderName}:{message}");  // 형식: MSG:발신자이름:메시지내용
                    AddLog($" [{senderName}({senderIP}) → {partnerIP}] {message}", Color.DarkGreen);
                }
            }
        }


        private List<string> GetBlockedIPs(string myIP)
        {
            List<string> blocked = new List<string>();

            try{
                using (MySqlConnection conn = new MySqlConnection(connectionDB))
                {
                    
                    conn.Open();

                    string query = "SELECT blocked_ip FROM block WHERE blocker_ip = @ip AND is_block = '1'";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@ip", myIP);

                  
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        // 모든 레코드를 순회하며 리스트에 추가
                        while (reader.Read())
                        {
                            blocked.Add(reader["blocked_ip"].ToString());
                        }
                    }

                    //  디버그 로그: 차단된 IP 목록
                    if (blocked.Count > 0)
                    {
                        AddLog($" [{myIP}]가 차단한 IP: {string.Join(", ", blocked)}", Color.Orange);
                    }
                }
            }
            catch (Exception ex)
            {
                
                AddLog($" GetBlockedIPs DB 오류 [{myIP}]: {ex.Message}", Color.Red);
                AddLog($"  스택 트레이스: {ex.StackTrace}", Color.Red);
            }

            return blocked;
        }

     
        private bool IsBlocked(string ip1, string ip2)
        {
            try {
                using (MySqlConnection conn = new MySqlConnection(connectionDB))
                {
                    
                    conn.Open();

                    string query = "SELECT COUNT(*) FROM block WHERE blocker_ip = @ip1 AND blocked_ip = @ip2 AND is_block = '1'";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@ip1", ip1);
                    cmd.Parameters.AddWithValue("@ip2", ip2);

                    // 레코드가 1개 이상이면 차단된 것
                    bool isBlocked = Convert.ToInt32(cmd.ExecuteScalar()) > 0;

                    // 디버그 로그: 차단 체크 결과
                    if (isBlocked)
                    {
                        AddLog($" 차단 확인: [{ip1}]가 [{ip2}]를 차단함", Color.Orange);
                    }

                    return isBlocked;
                }
            }
            catch (Exception ex)
            {
                AddLog($" IsBlocked DB 오류 [{ip1}→{ip2}]: {ex.Message}", Color.Red);
                return false;
            }
        }

       
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            // 서버가 실행 중이면 중지
            if (isRunning)
            {
                StopServer();
            }
        }

     
        private void button1_Click_1(object sender, EventArgs e)
        {
            if (!isRunning)
            {
                // 서버가 중지 상태 → 시작
                StartServer();
            }
            else
            {
                // 서버가 실행 중 → 중지
                StopServer();
            }
        }
    }
}
