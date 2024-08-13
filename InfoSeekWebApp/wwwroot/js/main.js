// Importar módulos necesarios
import { auth, provider, signInWithPopup, getRedirectResult, signOut } from './firebase-config.js';
import { onAuthStateChanged } from "https://www.gstatic.com/firebasejs/9.17.1/firebase-auth.js";

// Verificar el estado de autenticación al cargar la página
onAuthStateChanged(auth, (user) => {
    if (user) {
        // Usuario autenticado, mostrar información
        displayUserInfo(user);
    } else {
        // Usuario no autenticado, mostrar opciones de autenticación
        document.getElementById('profile-section').style.display = 'none';
        document.getElementById('auth-options').style.display = 'block';
    }
});

// Función para abrir modales
function openModal(modalId) {
    var modal = document.getElementById(modalId);
    if (modal) {
        modal.style.display = 'block';
    } else {
        console.error('El modal con ID ' + modalId + ' no existe.');
    }
}

// Función para cerrar modales
function closeModal(modalId) {
    var modal = document.getElementById(modalId);
    if (modal) {
        modal.style.display = 'none';
    } else {
        console.error('El modal con ID ' + modalId + ' no existe.');
    }
}

// Función para manejar la autenticación con Google
function openGoogleAuth() {
    signInWithPopup(auth, provider)
        .then((result) => {
            console.log("Usuario autenticado:", result.user);
            displayUserInfo(result.user);
        })
        .catch((error) => {
            console.error("Error en la autenticación:", error);
        });
}

// Manejar el resultado de la redirección
getRedirectResult(auth).then((result) => {
    if (result && result.user) {
        displayUserInfo(result.user);
    } else {
        console.log('No hay resultados de autenticación disponibles.');
    }
}).catch((error) => {
    console.error("Error en la autenticación: ", error);
});

// Función para mostrar información del usuario
function displayUserInfo(user) {
    document.getElementById('profile-photo').src = user.photoURL;
    document.getElementById('user-name').innerText = user.displayName;
    document.getElementById('user-email').innerText = user.email;
    document.getElementById('profile-section').style.display = 'block';
    document.getElementById('auth-options').style.display = 'none';
}

// Función para cerrar sesión
function logout() {
    signOut(auth).then(() => {
        document.getElementById('profile-section').style.display = 'none';
        document.getElementById('auth-options').style.display = 'block';
    }).catch((error) => {
        console.error('Error al cerrar sesión:', error);
    });
}

// Función de sincronización de datos
function syncData() {
    alert('Datos sincronizados correctamente.');
}

// Función para abrir la configuración
function openSettings() {
    // Lógica para abrir la sección de configuración
}

// Función para abrir el historial
function openHistory() {
    window.location.href = "~/Pages/historial"; // Navega a la página de historial
}

// Función para manejar errores de Cross-Origin-Opener-Policy
window.addEventListener('message', function(event) {
    if (event.origin !== window.location.origin) {
        console.error('Cross-Origin-Opener-Policy error: No se puede cerrar la ventana.');
        return;
    }
});

// Exponer funciones globalmente
window.openGoogleAuth = openGoogleAuth;
window.openModal = openModal;
window.closeModal = closeModal;
window.logout = logout;
window.openSettings = openSettings;
window.openHistory = openHistory;
