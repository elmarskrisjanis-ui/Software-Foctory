# AGENTS.md

## 1. Mērķis un darbības joma

Šis fails nosaka kopīgās tehnoloģiju, drošības un izstrādes instrukcijas kodēšanas aģentam. Novieto to repozitorija saknē. Piemēro prasības jaunam kodam, labojumiem, migrācijām, konfigurācijai un CI/CD izmaiņām. Konkrētā risinājuma specifikācija nosaka biznesa funkcijas, datus, lomas, integrācijas un pieņemšanas kritērijus; tā var noteikt stingrākas prasības, bet nedrīkst vājināt šo bāzi bez dokumentēta organizācijas apstiprinājuma.

- **OBLIGĀTI** nozīmē, ka prasību izpildi tās piemērošanas jomā. Ja vajadzīgs ārējs pakalpojums vai apstiprinājums, dokumentē atkarību un neuzrādi prasību kā izpildītu bez pierādījuma.
- **PĒC NOKLUSĒJUMA** apzīmē sākotnējo tehnoloģiju izvēli. Izmanto citu risinājumu tikai ar dokumentētu organizācijas apstiprinājumu. Ja esošā konfigurācija atšķiras, neveic migrāciju patvaļīgi: norādi atšķirību un pārbaudi apstiprinājumu.
- Ja prasības konfliktē vai trūkst informācijas, kas ietekmē drošību, piekļuvi vai datu apstrādi, neveic skarto izmaiņu, kamēr jautājums nav atrisināts. Turpini neatkarīgo, droši izpildāmo darbu.

## 2. Noklusējuma tehnoloģiskā bāze

Tabulā norādītās tehnoloģijas piemēro tikai komponentēm, kas nepieciešamas konkrētajam risinājumam. Nepievieno datubāzi, API, frontend vai citus komponentus tikai tāpēc, ka tie uzskaitīti tabulā.

| Joma | Sākotnējā izvēle |
| --- | --- |
| Frontend | React ar Next.js  |
| Backend | .NET 10 (`net10.0`). Ja projektā izmanto `global.json`, fiksē tajā sandbox pieejamo, organizācijā atļauto .NET 10 SDK versiju. Ja atbilstošs SDK nav pieejams, norādi trūkstošo priekšnosacījumu un nepārej uz citu .NET galveno versiju bez apstiprinājuma. |
| Datubāze | PostgreSQL 18 |
| API | REST ar OpenAPI 3.x. Uzturi aprakstu saskaņā ar faktisko API uzvedību. |
| Konteinerizācija | Docker/OCI konteineri |
| Lokālā vide | Docker Compose (`docker compose`) |
| Repozitorijs | Atsevišķs GitLab projekts; ja risinājumam vajadzīgi vairāki projekti, izmanto atsevišķu GitLab grupu. |
| Datubāzes izmaiņas | Versētas migrācijas. Nepārraksti jau piemērotas migrācijas; datu zuduma riska gadījumā dokumentē pārbaudes un atjaunošanas ceļu. |
| Žurnāli | Strukturēti lietojumprogrammas un auditācijas žurnāli ar nošķirtām piekļuves un glabāšanas prasībām |
| Konfigurācija | Centralizēta konfigurācijas un slepeno vērtību pārvaldība. Repozitorijā glabā konfigurācijas piemērus bez īstām slepenajām vērtībām. |

## 3. Pirmais posms: lokāla izstrāde izolētā sandbox vidē

- Pirmajā izstrādes posmā visas aģenta darbības **OBLIGĀTI** veic tikai lokālā, izolētā sandbox vidē. Sandbox robežām jānovērš neatļauta piekļuve organizācijas iekšējām sistēmām, koplietojamiem resursiem un citām vidēm.
- Neveic izvietošanu DEV, TEST vai PROD vidē un neveic izmaiņas ārējās sistēmās, organizācijas infrastruktūrā, identitātes platformā, tīkla konfigurācijā vai citos koplietojamos resursos. Šīs darbības drīkst veikt tikai vēlākā posmā pēc atsevišķa, skaidra pilnvarojuma.
- Neizmanto produkcijas datus, reālus personas datus, īstas paroles, piekļuves tokenus, API atslēgas, sertifikātu privātās atslēgas vai citus produkcijas akreditācijas līdzekļus. Izmanto sintētiskus vai anonimizētus testa datus un drošas piemēra vērtības.
- Kodēšanas aģents darbojas lokālā, izolētā sandbox vidē. Aģentam ir atļauta tikai tā izejošā HTTPS tīkla piekļuve, kas nepieciešama tā darbībai ar organizācijas apstiprināto ChatGPT Business/OpenAI pakalpojumu. Cita ārējā tīkla piekļuve pēc noklusējuma nav atļauta. Šī atļauja neattiecas uz lietotnes tīkla piekļuvi vai atkarību lejupielādi. Būvēšanai un testēšanai izmanto tikai sandbox jau pieejamus SDK, NuGet un npm pakotnes, konteineru attēlus un rīkus. Ja kā trūkst, norādi precīzu nosaukumu un vajadzīgo versiju (ja zināma), skarto darbību un turpini nebloķētos darbus. Nemaini tīkla iestatījumus un neapej ierobežojumus.
- Piekļuve ChatGPT Business/OpenAI pakalpojumam ir paredzēta tikai kodēšanas aģenta darbībai un nav izstrādājamā risinājuma arhitektūras, izpildvides vai ārējo integrāciju daļa.
- Izstrādājamais risinājums **NEDRĪKST** izmantot OpenAI, ChatGPT, LLM vai citus ārējos mākslīgā intelekta pakalpojumus, ja vien konkrētā risinājuma specifikācijā tas nav nepārprotami noteikts un atsevišķi apstiprināts.
- Aģents nedrīkst pievienot izstrādājamajam risinājumam OpenAI SDK, API integrāciju, LLM bibliotēkas, modeļu piekļuves atslēgas vai citus mākslīgā intelekta komponentus tikai tādēļ, ka pats kodēšanas aģents izmanto ChatGPT Business/OpenAI pakalpojumu.
- Izstrādājamā risinājuma ārējās integrācijas nosaka tikai konkrētā risinājuma specifikācija. Ja uzdevuma izpildei vajadzīga cita ārējā tīkla piekļuve, aģents to neveido patvaļīgi, bet dokumentē kā neatrisinātu atkarību vai pieprasāmu izņēmumu.
- Ārējās integrācijas un organizācijas pakalpojumus, kas lokālajā vidē nav pieejami, aizstāj ar mock, stub, emulatoru vai citu kontrolētu testa aizvietotāju. Skaidri dokumentē, kura uzvedība ir imitēta un kas vēl jāpārbauda reālā integrācijas vidē. Ja uzdevums skar autentifikāciju, lokāli lieto skaidri nošķirtu testa identitāti un pārbaudi atļautu un neatļautu piekļuvi servera pusē. Testa aizstājējs nedrīkst būt ieslēdzams produkcijā. Lokālie testi neapliecina reālā VID SSO, MFA vai identitātes platformas sesiju atsaukšanas darbību.
- Risinājumam jābūt lokāli palaižamam, būvējamam un testējamam. Nodrošini palaišanas norādījumus, konfigurācijas piemērus bez slepenām vērtībām, atkarību aprakstu un, ja attiecināms, Docker Compose konfigurāciju.
- Lokālajā sandbox vidē palaid attiecināmo būvēšanu, automatizētos testus un pieejamās drošības pārbaudes. Ja kādu pārbaudi nav iespējams palaist, norādi iemeslu un neuzrādi to kā sekmīgi izpildītu.
- Pirmā posma rezultāts ir lokāli strādājošs un pārbaudāms risinājums. Tas nav uzskatāms par gatavu izvietošanai organizācijas DEV, TEST vai PROD vidē. Atzīmē uzdevumu kā pabeigtu tikai tad, ja risinājums ir faktiski lokāli uzbūvēts un palaists un ir pārbaudīti specifikācijas pieņemšanas kritēriji. Ja obligātā lokālā pārbaude nav izpildāma trūkstoša priekšnosacījuma dēļ, norādi statusu “Bloķēts”, trūkstošo priekšnosacījumu un kas tomēr ir pārbaudīts. Neizturēta pārbaude nozīmē, ka uzdevums nav pabeigts. Vēlākam posmam paredzētās pārbaudes uzskaiti atsevišķi; to neizpilde pati par sevi nebloķē lokālā posma pabeigšanu.
- Darba beigās dokumentē: kas izveidots vai mainīts; kā risinājumu lokāli palaist; kādas pārbaudes faktiski izpildītas un ar kādu rezultātu; kuras integrācijas aizstātas ar mock vai stub; kas nav pārbaudīts; kādi jautājumi vai priekšnosacījumi nepieciešami nākamajam posmam.

### Prasības nākamajam posmam

- **VID SIEM integrāciju, auditācijas žurnālu ilgtermiņa glabāšanu un GitLab projekta un CI/CD konfigurēšanu** saglabā kā nākamā posma prasības. Pirmajā lokālajā sandbox posmā tās neievies, nepieslēdz un nepārbaudi ārējā vidē. Šis posma ierobežojums ir noteicošais, ja citās sadaļās šīs darbības aprakstītas kā obligātas. Uz nākamo posmu atliek arī reālā VID SSO un MFA pieslēgšanu, identitātes platformas aktīvo sesiju atsaukšanas pārbaudi, VID slepeno vērtību glabātuves pieslēgšanu, organizācijas infrastruktūrā nodrošinātās šifrēšanas pārbaudi un VID incidentu nodošanas kanāla pārbaudi. Lokāli izmanto tikai drošas konfigurācijas piemēra vērtības; neizdomā iekšējo pakalpojumu parametrus. Neatslēdz sertifikātu validāciju un nevājini lietojumprogrammas drošības kontroles.
- Izstrādājot risinājumu, saglabā iespēju šīs prasības īstenot vēlāk: veido strukturētus auditācijas notikumus un dokumentē lokālās būvēšanas un testēšanas komandas, ko vēlāk var izmantot CI/CD. Norādi atliktos darbus un nepieciešamos priekšnosacījumus; neuzrādi tos kā izpildītus.
- Pārējās drošības prasības, sandbox izolāciju, lokālo būvēšanu, testēšanu un pieejamās lokālās drošības pārbaudes turpini piemērot atbilstoši to tvērumam.

## 4. Identitāte, piekļuve un šifrēšana

- Nodrošini datu šifrēšanu pārraidē un glabāšanā. Izmanto HTTPS un TLS 1.2 vai augstāku organizācijā apstiprinātu TLS versiju. Neatslēdz sertifikātu validāciju. Dokumentē, kur pārraides un glabāšanas šifrēšanu nodrošina infrastruktūra.
- Integrē autentifikāciju ar organizācijas vienotās pierakstīšanās risinājumu (SSO). MFA jābūt obligātai gan administratoriem, gan lietotājiem. Ja to nodrošina identitātes platforma, dokumentē attiecīgo politiku un tās pārbaudes veidu. Lokāli testa aizstājēji nedrīkst būt ieslēdzami produkcijā.
- Ievies RBAC, mazāko privilēģiju principu un pienākumu nodalīšanu. Atļaujas pārbaudi servera pusē, arī API līmenī un konkrētam resursam, ja piekļuve ir atkarīga no tā. Lietotāja saskarnē paslēpta poga nav piekļuves kontrole.
- Aizsargā API galapunktus pēc noklusējuma. Publisku galapunktu atļauj tikai tad, ja tas nepārprotami paredzēts specifikācijā; dokumentē to OpenAPI aprakstā un pārbaudi, ka tas neatklāj aizsargātus datus.
- Nodrošini iespēju nekavējoties bloķēt vai atsaukt lietotāju, administratoru un API piekļuvi. Pārbaudi, ka atsaukšana iedarbojas arī uz jau aktīvām sesijām un izsniegtiem piekļuves akreditācijas līdzekļiem. Nepaļaujies tikai uz to derīguma termiņa beigām. Ja platforma to nevar nodrošināt, norādi neatbilstību.
- Ārējām sistēmām izmanto tikai drošus, autentificētus un šifrētus savienojumus. Integrāciju piekļuves tiesības ierobežo līdz konkrētajai funkcijai nepieciešamajam.

## 5. Konfigurācija un slepenās vērtības

- Neievieto paroles, API atslēgas, piekļuves tokenus, privātās atslēgas vai citus autentifikācijas noslēpumus pirmkodā vai GitLab repozitorijā, tostarp tā vēsturē. Neiekļauj tos arī konteineru attēlos, testu datos, CI/CD izvadē, kļūdās vai žurnālos.
- Īstos autentifikācijas noslēpumus glabā organizācijas apstiprinātā, aizsargātā slepeno vērtību pārvaldības risinājumā. Lietojumprogrammai piešķir tikai nepieciešamo piekļuvi.
- Nodali izstrādes, testa un produkcijas konfigurācijas un slepenās vērtības. Repozitorijā norādi vajadzīgo parametru nosaukumus un drošas piemēra vērtības. Ja obligātie drošības iestatījumi trūkst, lietojumprogramma nedrīkst pārslēgties uz nedrošu konfigurāciju.

## 6. Auditācija un incidenti

- Auditē veiksmīgus un neveiksmīgus pieslēgumus, atteiktus piekļuves mēģinājumus, piekļuves tiesību izmaiņas, administratoru darbības, būtisku datu skatīšanu, eksportu un dzēšanu, kā arī konfigurācijas izmaiņas. Risinājuma specifikācijā identificē auditējamos datus un darbības. Ja pieslēgumus auditē SSO platforma, dokumentē šo notikumu piegādes ceļu līdz VID SIEM.
- Veido strukturētus auditācijas notikumus: UTC laiks, notikuma veids, darbības veicēja identifikators, ja zināms un atļauts, mērķa identifikators, ja attiecināms, iznākums un korelācijas identifikators. Neiekļauj sensitīvu saturu, paroles, tokenus, API atslēgas vai citus autentifikācijas noslēpumus. Nežurnalē pilnas pieprasījumu vai atbilžu datnes un saturu pēc noklusējuma.
- Aizsargā auditācijas žurnālus pret nesankcionētu labošanu un dzēšanu. Nodali rakstīšanas, lasīšanas un pārvaldības atļaujas; lietojumprogrammas identitātei nepiešķir tiesības labot vai dzēst saglabātos ierakstus.
- Nodrošini auditācijas eksportu mašīnlasāmā formātā uz VID SIEM, izmantojot saskaņoto formātu un piegādes kanālu. Dokumentē saskarni un to, kā pārbaudīt notikumu saņemšanu. Eksporta kļūme nedrīkst palikt nepamanīta.
- Nodrošini auditācijas žurnālu glabāšanu vismaz **18 mēnešus** vai dokumentētu mehānismu, ar kuru VID var nodrošināt šādu glabāšanas termiņu. Norādi, kur glabāšanas politika tiek piemērota un kas to pārbauda.

## 7. Droša ievaddatu un kļūdu apstrāde

- Servera pusē validē visus datus no lietotājiem, failiem, API un ārējām sistēmām pirms apstrādes. Pārbaudi tipu, formātu, izmēru un atļautās vērtības; failiem un integrāciju atbildēm piemēro konkrētajai saskarnei noteiktos ierobežojumus.
- Aizsargā risinājumu pret izmantotajai tehnoloģijai aktuāliem uzbrukumiem, tostarp SQL Injection, Command Injection, XSS, Path Traversal un SSRF. Izmanto parametrizētus datubāzes vaicājumus vai līdzvērtīgu drošu mehānismu. Izvairies no čaulas komandām ar ārēju ievadi, kodē izvadi atbilstoši kontekstam, ierobežo failu ceļus līdz atļautajai glabātuvei un kontrolē servera izejošo pieprasījumu galamērķus, ja tos ietekmē ārēja ievade.
- Lietotājiem un API klientiem atgriez kontrolētu kļūdas atbildi. Neatklāj steka izsekojumu (`stack trace`), autentifikācijas noslēpumus, iekšējos failu ceļus vai citu nevajadzīgu tehnisko informāciju. Diagnostikai izmanto korelācijas identifikatoru un aizsargātus žurnālus.

## 8. Atkarības, CI/CD un drošības testi

- Izmanto atbalstītas programmatūras, ietvaru, bibliotēku un citu komponenšu versijas. Deklarē tiešās atkarības, kontrolē versijas un izmanto fiksācijas failus (`lockfiles`), ja attiecīgais rīks tos atbalsta. Neizmanto komponentes ar zināmu kritisku ievainojamību, ja pieejama droša un atbalstīta versija. Ja tādas nav, dokumentē risku un nodod to izvērtēšanai; neapstiprini izņēmumu pats.
- Nodrošini, ka GitLab CI/CD var palaist būvēšanu, testus, SAST, SCA jeb atkarību skenēšanu, slepeno vērtību skenēšanu un ievainojamību skenēšanu attiecīgajiem artefaktiem, tostarp konteineru attēliem. Izmanto organizācijā apstiprinātos rīkus vai saskaņotās CI veidnes. Ja rīks vai piekļuve nav pieejama, dokumentē trūkstošo priekšnosacījumu.
- Izmaiņām paredzi samērīgus pozitīvos un negatīvos testus. Pārbaudi autentifikāciju, lomu un resursu robežas, atsaukšanu, nederīgus ievaddatus, auditācijas notikumus un sensitīvu datu neesamību žurnālos, ja attiecīgā kontrole ir skarta.
- Neatkarīgu drošības un ielaušanās testu apjomu nosaka atbilstoši risinājuma riskam. Sagatavo neatkarīgajiem testētājiem sistēmas robežu, saskarņu un drošības kontroļu aprakstu. Aģenta veiktās pārbaudes neaizstāj neatkarīgu testu.

## 9. Aģenta darba kārtība un pabeigtības kritēriji

1. **Pirms izmaiņām** izlasi risinājuma specifikāciju, repozitorija instrukcijas un esošos testus. Nosaki, kuras bāzes prasības izmaiņa skar. Neizdomā VID SSO, SIEM, slepeno vērtību glabātuves vai citu iekšējo pakalpojumu parametrus.
2. **Īstenojot** veic tikai uzdevumam vajadzīgās izmaiņas. Ievēro *secure by design* un *secure by default*. Neapej un nevājini drošības kontroli, lai realizētu funkciju, novērstu tehnisku problēmu vai panāktu testa izdošanos. Ja kontrole bloķē darbu, novērs cēloni vai dokumentē neatrisināto jautājumu.
3. **Pārbaudot** palaid pieejamās attiecināmās pārbaudes: būvēšanu, formatēšanu, testus, drošības pārbaudes un, ja skarts, OpenAPI līguma vai datubāzes migrāciju pārbaudi. Nelieto produkcijas datus vai akreditācijas līdzekļus lokālajās pārbaudēs. Bez skaidra pilnvarojuma neveic produkcijas izvietošanu, migrācijas vai konfigurācijas izmaiņas.
4. **Nododot** atjaunini attiecināmo dokumentāciju un uzskaiti: kas mainīts, kas faktiski palaists un ar kādu rezultātu, kas nav pārbaudīts un kādas atkarības vai neatbilstības palikušas. Skaidri nošķir lietojumprogrammā ieviesto, infrastruktūrā konfigurējamo un organizatoriski nodrošināmo. Neatzīmē nepārbaudītu kontroli kā izpildītu un nesauc risinājumu par gatavu produkcijai, ja obligātās kontroles vēl jāpierāda.
