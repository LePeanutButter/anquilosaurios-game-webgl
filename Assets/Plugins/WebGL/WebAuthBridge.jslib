// Archivo: Assets/Plugins/WebGL/WebAuthBridge.jslib

mergeInto(LibraryManager.library, {
    
    /**
     * Registra un listener para recibir mensajes desde el navegador padre
     */
    RegisterUnityMessageListener: function() {
        console.log('[Unity] Registrando listener para mensajes del navegador');
        
        window.addEventListener('message', function(event) {
            console.log('[Unity] Mensaje recibido:', event.data);
            
            // Verificar que el mensaje es del tipo correcto
            if (event.data && event.data.type === 'USER_AUTH_DATA') {
                console.log('[Unity] Datos de autenticación recibidos');
                
                // Crear JSON con los datos
                var jsonData = JSON.stringify({
                    token: event.data.token || '',
                    userName: event.data.userName || 'Player',
                    userEmail: event.data.userEmail || ''
                });
                
                console.log('[Unity] Enviando datos a Unity:', jsonData);
                
                // Llamar al método de Unity
                // IMPORTANTE: 'WebAuthReceiver' debe ser el nombre del GameObject que tiene el script
                // y 'ReceiveUserData' es el nombre del método público
                SendMessage('WebAuthReceiver', 'ReceiveUserData', jsonData);
            }
        });
        
        console.log('[Unity] Listener registrado exitosamente');
    },
    
    /**
     * Envía un mensaje al navegador padre (Svelte)
     */
    SendMessageToParent: function(messagePtr) {
        var message = UTF8ToString(messagePtr);
        console.log('[Unity] Enviando mensaje al navegador:', message);
        
        try {
            var messageObj = JSON.parse(message);
            window.parent.postMessage(messageObj, '*');
            console.log('[Unity] Mensaje enviado exitosamente');
        } catch (e) {
            console.error('[Unity] Error al enviar mensaje:', e);
        }
    }
    
});
