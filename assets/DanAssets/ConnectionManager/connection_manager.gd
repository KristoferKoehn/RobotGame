extends Node

var IP_ADDRESS = "50.47.173.115"
var PORT = 9002

var socket = WebSocketPeer.new()

var prevstate = WebSocketPeer.STATE_CLOSED

# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	socket.connect_to_url("wss://" + IP_ADDRESS + ":" + str(PORT))
	print(socket.get_connected_host())

# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta: float) -> void:
	socket.poll()
	var state = socket.get_ready_state()
	if state == WebSocketPeer.STATE_CONNECTING:
		if state != prevstate:
			print("connecting...")
	if state == WebSocketPeer.STATE_OPEN:
		if state != prevstate:
			print("OPEN")
		print("we get here")
		while socket.get_available_packet_count():
			print("Packet: ", socket.get_packet())
	elif state == WebSocketPeer.STATE_CLOSING:
		if state != prevstate:
			print("closing...")
		# Keep polling to achieve proper close.
		pass
	elif state == WebSocketPeer.STATE_CLOSED:
		if state != prevstate:
			print("closed")
		var code = socket.get_close_code()
		var reason = socket.get_close_reason()
		print("WebSocket closed with code: %d, reason %s. Clean: %s" % [code, reason, code != -1])
		set_process(false) # Stop processing.
		
	prevstate = state

func _on_submit_button_pressed() -> void:
	# get strings and shit
	var string = "test string"
	print(socket.get_ready_state())
	socket.put_packet(string.to_utf8_buffer())
