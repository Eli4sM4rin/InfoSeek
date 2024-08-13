import { initializeApp } from 'https://www.gstatic.com/firebasejs/9.17.1/firebase-app.js';
import { getFirestore, collection, addDoc, query, where, orderBy, getDocs, deleteDoc, doc, serverTimestamp } from 'https://www.gstatic.com/firebasejs/9.17.1/firebase-firestore.js';
import { getAuth, GoogleAuthProvider, signInWithPopup, signOut } from 'https://www.gstatic.com/firebasejs/9.17.1/firebase-auth.js';


const firebaseConfig = {
    apiKey: "AIzaSyDyfbvFtOK8H2MFFp7w7sxC2b0t4bfwzx8",
    authDomain: "infoseek-2ce33.firebaseapp.com",
    projectId: "infoseek-2ce33",
    storageBucket: "infoseek-2ce33.appspot.com",
    messagingSenderId: "624180305483",
    appId: "1:624180305483:web:188bd701861e279b13f7ce"
};

// Inicializar Firebase
const app = initializeApp(firebaseConfig);
const auth = getAuth(app);
const provider = new GoogleAuthProvider();
const db = getFirestore(app);

export { auth, provider, signInWithPopup, signOut, db, collection, addDoc, query, where, orderBy, getDocs, deleteDoc, doc, serverTimestamp };