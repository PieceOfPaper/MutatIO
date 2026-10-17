// WebGLWebSocketTransport가 사용하는 브라우저 WebSocket 브리지입니다.
var MutatIOWebSocketLibrary = {
  $MutatIOWebSocketState: {
    sockets: {},
    nextId: 1,
    onOpen: 0,
    onMessage: 0,
    onClose: 0
  },

  MutatIOWebSocket_SetCallbacks: function (onOpen, onMessage, onClose) {
    MutatIOWebSocketState.onOpen = onOpen;
    MutatIOWebSocketState.onMessage = onMessage;
    MutatIOWebSocketState.onClose = onClose;
  },

  MutatIOWebSocket_Connect: function (urlPtr) {
    var state = MutatIOWebSocketState;
    var url = UTF8ToString(urlPtr);
    var socket;

    try {
      socket = new WebSocket(url);
    } catch (e) {
      console.error('[MutatIO] WebSocket 생성 실패: ' + url, e);
      return 0;
    }

    var id = state.nextId++;
    socket.binaryType = 'arraybuffer';
    state.sockets[id] = socket;

    socket.onopen = function () {
      {{{ makeDynCall('vi', 'state.onOpen') }}}(id);
    };

    socket.onmessage = function (event) {
      var bytes;
      if (event.data instanceof ArrayBuffer) {
        bytes = new Uint8Array(event.data);
      } else if (typeof event.data === 'string') {
        bytes = new TextEncoder().encode(event.data);
      } else {
        return;
      }

      var ptr = _malloc(bytes.length);
      HEAPU8.set(bytes, ptr);
      try {
        {{{ makeDynCall('viii', 'state.onMessage') }}}(id, ptr, bytes.length);
      } finally {
        _free(ptr);
      }
    };

    socket.onclose = function (event) {
      delete state.sockets[id];
      {{{ makeDynCall('vii', 'state.onClose') }}}(id, event.code);
    };

    return id;
  },

  MutatIOWebSocket_Send: function (id, dataPtr, length) {
    var socket = MutatIOWebSocketState.sockets[id];
    if (!socket || socket.readyState !== WebSocket.OPEN) {
      return 0;
    }

    socket.send(HEAPU8.slice(dataPtr, dataPtr + length));
    return 1;
  },

  MutatIOWebSocket_Close: function (id, code) {
    var socket = MutatIOWebSocketState.sockets[id];
    if (!socket) {
      return;
    }

    try {
      socket.close(code);
    } catch (e) {
      console.warn('[MutatIO] WebSocket 종료 실패', e);
    }
  }
};

autoAddDeps(MutatIOWebSocketLibrary, '$MutatIOWebSocketState');
mergeInto(LibraryManager.library, MutatIOWebSocketLibrary);
