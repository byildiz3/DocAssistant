import { useRef,useState } from 'react'
import heroImg from './assets/hero.png'
import reactLogo from './assets/react.svg'
import viteLogo from './assets/vite.svg'
import './App.css'

const API="https://localhost:7199";
 
 export default function App(){
  const [docText,setDocText]= useState("");
  const [uploadMsg,setUploadMsg]=useState("");
  const [question,setQuestion]=useState("");
  const [answer,setAnswer]=useState("");
  const [loading,setLoading]=useState(false);
  const sourceRef=useRef<EventSource | null>(null);
 async function uploadDocument() {
    setUploadMsg("Yükleniyor..");
    try {
      const res = await fetch(`${API}/documents`,{
        method:"POST",
        headers:{"Content-Type":"application/json"},
        body:JSON.stringify({text:docText})
      });
      if(!res.ok) throw new Error(`HTTP ${res.status}`);
      const data: {chunkCount:number} = await res.json();
      setUploadMsg(`${data.chunkCount} parça kaydedildi.`);
    } catch (error) {
      setUploadMsg(`Hata: ${(error as Error).message}`);
    }
  }

  function ask(){
    sourceRef.current?.close();
    setAnswer('');
    setLoading(true);

    const es = new EventSource(`${API}/ask?q=${encodeURIComponent(question)}`);
    sourceRef.current = es;
    let gotData = false;

    es.onmessage = (e) => {
      gotData = true;
      setAnswer((prev) => prev + e.data);
    };

    es.onerror = () => {
      es.close();
      setLoading(false);
      if(!gotData) setAnswer("Bağlantı hatası")
    }
 
  }

  return(
    <main className='container'>
      <h1>DocAssistant</h1>
      <h2>1. Doküman yükle</h2>
      <textarea   rows={6} value={docText} onChange={(e) => setDocText(e.target.value) } placeholder='Metni buraya yapıştır..'></textarea>
      <p>{docText.length} karakter</p>
      <button onClick={uploadDocument} disabled={!docText.trim()}>
        Yükle
      </button>
      <p>{uploadMsg}</p>
      <h2>2. Soru sor</h2>
      <input value={question} onChange={(e) => setQuestion(e.target.value) } placeholder='Sorunu yaz..' />
      <button onClick={ask} disabled={!question.trim() || loading}>{loading ? "Cevaplanıyor" : "Sor"}</button>
      <p>Cevap:</p>
      <div className='answer'>{answer}</div>
    </main>

  )
 }
