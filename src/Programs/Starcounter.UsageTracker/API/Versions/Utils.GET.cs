using System;
using Starcounter;
using Starcounter.Advanced;
using StarcounterApplicationWebSocket.VersionHandler.Model;
using Codeplex.Data;
using Starcounter.Applications.UsageTrackerApp.VersionHandler;
using StarcounterApplicationWebSocket.VersionHandler;

namespace StarcounterApplicationWebSocket.API.Versions {
    internal class Utils {
        public static void BootStrap(ushort port) {

            Handle.GET(port, "/reset", (Request request) => {

                Db.Transaction(() => {

                    LogWriter.WriteLine(string.Format("Reseting database."));

                    var result = Db.SlowSQL("SELECT o FROM VersionSource o");

                    foreach (VersionSource item in result) {
                        item.Delete();
                    }

                    result = Db.SlowSQL("SELECT o FROM VersionBuild o");
                    foreach (VersionBuild item in result) {
                        item.Delete();
                    }

                    result = Db.SlowSQL("SELECT o FROM Somebody o");
                    foreach (Somebody item in result) {
                        item.Delete();
                    }

                    // Reset settings
                    VersionHandlerSettings settings = VersionHandlerApp.Settings;
                    settings.Delete();
                    VersionHandlerApp.Settings = VersionHandlerSettings.GetSettings();
                    LogWriter.Init(VersionHandlerApp.Settings.LogFile);

                });

                if (AssureEmails() == false) {
                    return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.InternalServerError, Body="Could not generate unique id, email import aborted" };
                }

                return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.NoContent };

            });


            Handle.GET(port, "/refresh", (Request request) => {

                LogWriter.WriteLine(string.Format("Refresh database."));

                VersionHandlerApp.Settings = VersionHandlerSettings.GetSettings();

                // Set log filename to logwriter
                LogWriter.Init(VersionHandlerApp.Settings.LogFile);

                SyncData.Start();

                VersionHandlerApp.UnpackWorker.Trigger();
                VersionHandlerApp.BuildkWorker.Trigger();
                return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.NoContent };

            });

        }


        /// <summary>
        /// Quick way to import emails
        /// </summary>
        private static bool AssureEmails() {

            var result = Db.SlowSQL("SELECT o FROM Somebody o").First;
            if (result != null) return true;

            string str = "xixan98@live.com;sir_cris2010@hotmail.com;h.peter.eriksson@gmail.com;himanshu.singh24x7@gmail.com;gery2301@gmail.com;kartono.mgt@gmail.com;sivasakthivel.palani@accenture.com;elvuel@gmail.com;fisayoakintade@yahoo.com;waterlilyart@gmail.com;warship2000@gmail.com;marcin@nextgen.pl;mekewijaya@yahoo.com;sajjad.raza@kse.com.pk;henrywong94@hotmail.com;raul_apaza@yahoo.com;henry.lu@analyticservice.net;lennard.cornelis@ing.nl;okwuagwucne@yahoo.com;xoxoyouknowitxoxo@hotmail.com;purdypervert@gmail.com;geneellis@ctc.net;son.doan@niteco.se;spatacus1988@yahoo.com;dohoangtung@gmail.com;teentitan1986@gmail.com;christianholmstrand@hotmail.com;contacto@lascarrascas.es;moody.20082010@gmail.com;buyerfromus@Hotmail.com;robert@uniqueue.se;tinawinston@valleytruckequipment.com;info@iprog.nl;borja.andres@gmail.com;ramonnaka@gmail.com;raja.s474@gmaIl.com;s_e_s_m@yahoo.com;lhbib95@gmail.com;normy273@hotmail.com;loulette999@live.fr;marcello.drewanz@wissens.com.br;kwcqgn@hcwcob.com;viswanath.etikala@gvkbio.com;sairam1806@gmail.com;uran3983@gmail.com;Pablo@prsoft.com;waytomyworld682@gmail.com;jcho95@hotmail.com;ola@sambergs.se;chettiars@dnbcb.com;benedetto.mcdonald@gmail.com;tracy.bowman@crye-leike.com;ville.laitila@mingraph.com;victor_augusto.gabriel@hotmail.com;siva.chimmani@gmail.com;xmr088@gmail.com;ivan.bolcina@apida.si;hitechmca@gmail.com;wesikc@knfycs.com;taylordubose@yahoo.com;peter.derwa@selfservicecompany.com;obantanahal@2xprime.com;skoyalkar@primetgi.com;joel@loop54.se;kingkero@gmail.com;harish217@yahoo.com;esmeralda.frank.castillo@gmail.com;nigel.alfred@gmail.com;abiiid@hotmail.com;kinijenika@gmail.com;wsf0001@auburn.edu;kqeulg@akqxmm.com;wxy2ab@sina.com;joakim.wennergren@neovici.se;lukasz.ciszak@hp.com;dyzhbb@bdtlup.com;jude.rae@gmail.com;johan.linnerhed@saabgroup.com;wallner.bg@gmail.com;lhussey@stonebond.com;cejchoj@gmail.com;hatsoykan@hotmail.com;costa@halicea.com;emilian.ionascu@think2solve.com;fahadcse07@gmail.com;adam@enigmaticflare.co.uk;val-imag@hotmail.com;geoffrey_forster@scientia.com;mr@magoo.fr;ph@realtime.co.th;rb@selligent.com;804193721@qq.com;happydvd@rediff.com;bluebalas@hotmail.com;flashkamilfia@ymail.com;info@4by4carhirekenya.com;gtorresrodriguez@gmail.com;gbanta@highspeed-solutions.net;hcullen01@gmail.com;pradeep4405@gmail.com;dear_angel0702@hotmail.com;lfav2012@gmail.com;djmzcin@yahoo.com;Don.whittaker@bestbuy.com;johnf442@aol.com;johne291@aol.com;alex.meyergleaves@gmail.com;dcdebock@gmail.com;ashesh1708@gmail.com;yuriaddict12345@aol.com;jingkun.lee@gmail.com;luismontbau@gmail.com;riccardo.tarli@txtgroup.com;starcounter@mailinator.com;abergheim@lodestonesolutions.com.au;peter@a2c.se;ssmart90@ymail.com;jeremychild@gmail.com;snezana.mitkova@yahoo.com;csavemark@hotmail.com;shurik@mail.zp.ua;sebastien-cote@hotmail.com;gre@feas.com;dbfuzz@hotmail.com;devendrakatuke@rediffmail.com;tomnuen@agiper.com;johna190@aol.com;johng233@aol.com;allan.udt@gmail.com;peter.schuermans@selfservicecompany.com;serdardirican@gmail.com;familyfun5@gmail.com;Yy@yy.com;patrick@videofy.me;Mhaerojo@yahoo.com;jazzyrainfire@gmail.com;johng23@aol.com;johna305@aol.com;s.limonta@kline.it;bjorn@edgrenalden.se;support@saba7host.com;Takanosaga999@gmail.com;lesbitcoil@gmail.com;spadsau@hotmail.com;ergun.ozyurt@joygame.com;marisolsantizo27@yahoo.com;g.saldi@kline.it;rtsucf@xztmbt.com;himanshu.solanki999@gmail.com;byusuk@gmx.de;umaribrahimawan@gmail.com;dave_cedric02@yahoo.com;info@artanddesign.com.gr;fleece@mail.ru;bjarne.christiansen@gmail.com;Venustark@gmail.com;danielchapman221@gmail.com;nikovv@gmail.com;darren_maglalang@yahoo.com;nuliknol@gmail.com;guitar_junkie_0912@yahoo.com;lovely_s12@yahoo.com;mxqueerco@gmail.com;jugqoh@qxvgyj.com;canbenegative@gmail.com;magnus@xsockets.net;samdei73@yahoo.com;sudhakar@fabergent.com;bb@millermalta.com;uzi_hiers@yahoo.co.uk;bhunter@innovasmarttechnologies.com;megumi_dwc@hotmail.com;aju_aju88@hotmail.com;dr0zaxx@gmail.com;bstearns@globalfinancialdata.com;www.sindhushankar@gmail.com;donyinaeric50@yahoo.com;SRHINGE1082@GMAIL.COM;jribeirosilva80@gmail.com;test3@test5.com;asa.holmstrom@starcounter.com;test3@test4.com;Nniizzaa03@gmail.com;mavullayya@gmail.com;everettsh1@gmail.com;sathyaco2000@gmail.com;mit.deepan@gmail.com;sjpzu@vodamail.co.za;myatkyawthu85@gmail.com;jani_26@abv.bg;habiburkhan98@yahoo.com;ddd@ddd.com;ccc@ccc.com;bbb@bbb.com;aaa@aaa.com;jckodel@gmail.com;gibbij@gmail.com;aaaa@aaa.com;alaaabdelatif1961@msn.com;omkingsanctuary9@gmail.com;sarazamora_24@hotmail.com;wayne.vos@gmail.com;Hanna.Nilsson@societyobjects.com;niklas@societyobjects.com;niklas.bjorkman@societyobjects.com;iliasushaibu@yahoo.com;kartikay2796@gmail.com;katikay2793@gmaol.com;jimmontross@gmail.com;baresaidoo@gmail.com;nopijambak@yahoo.com;per.jorlind@me.com;cbr@appearnetworks.com;yd0odd@yahoo.com;mallesh.santu@gmail.com;jitamitra.giet@gmail.com;natalia.szymacha@gmail.com;dcm-starcounter@capital-ideas.com;gabisreal@yahoo.com;archanad83@rocketmail.com;kumar.4gid@gmail.com;kirtisiwach@gmail.com;jsfnhngnnshaanika178@gmail.com;vishalbhormum@countryclub.com;yassinar_64@yahoo.com;excursion.hemant@gmail.com;jhal91@yahoo.com;eldani688@yahoo.co.id;chadani@sglg.asia.lk;don.cornforth@acgsystem.com;don.cornforth@gmail.com;rondro06@yahoo.fr;noli_inter@hotmail.com;SRIMARYANI21@ROCKETMAIL.COM;rahmat_kodja@yahoo.com;mohmed_f322@yahoo.com;www.yogaharjito@yahoo.co.id;innersakina@gmail.com;vinay@revinate.com;yanrkits@gmail.com;hakimsalim4@gmail.com;ad66@ukr.net;babyqurllovex3@aol.com;Dynakamalia@yahoo.com;d.nerada@shaw.ca;kmreddy_lichfl@rediffmail.com;subhash123456789smailbox@rediffmail.com;mun_bali@yahoo.com;silichungweekly@gmail.com;waldyhester@gmail.com;eman_super@yahoo.com;brainwaves01@hotmail.com;nurjahan.nesa@yahoo.com;anilpatel.ladol@gmail.com;mal873999@live.com;weix77@gmail.com;vivi.anne@windowslive.com;frel8817@gmail.com;frel8817@gmail.com;frel8817@gmail.com;frel8817@gmail.com;frel8817@gmail.com;frel8817@gmail.com;frel8817@gmail.com;26@example.com;prarom2011@hotmail.com;25@example.com;kostis.malkogiannis@starcounter.com;kostis@starcounter2.com;jadeebabes@hotmail.com;kamis2829.KHM@gmail.com;dbs101dbs@gmail.com;arielram@gmail.com;headline1118@hotmail.com;Xcunamix@gmail.com;kostasdre@yahoo.com;michaelayers@yahoo.com;v20jeanpiero20@hotmail.com;Masud787mbs@yahoo.com;vivimusi@gmail.com;umin.msmedia@gmail.com;Abanob.boney@yahoo.com;jan.berggren@neovici.se;regentblogs@gmail.com;lillyinfinfinito@gmail.com;hour_hour16872@yahoo.com;Chou_d500@yahoo.fr;ebruli93@gmail.com;Sreylenkhath@yahoo.com;tareqsaid19@hotmail.com;santi_espi85@hotmail.com;W002012@hotmail.com;Kadek1985@yahoo.co.id;sudhir.bhande@yahoo.com;johan.bergens@gmail.com;Franciscoalmeida1986@hotmail.com;wufong22@hotmail.com;suwat_wat1980@hotmail.com;perezmichael17@aol.com;Yinetjaquez032@hotmail.com;sayyadrizwanali@yahoo.co.in;communitydevelopment46@gmail.com;popolay629@gmail.com;manvendra.patel1@gmail.com;blackbutterfly.p95@gmail.com;jess_041581@yahoo.com;a_allam100@hotmail.com;mohamed.love88@yahoo.com;arshadbhutta@rockemail.com;martenc@gmail.com;soowaijian@hotmail.com;tinio2net@yahoo.com;asd@yhaoo.com;raaj_001@yahoo.co.uk;biltu.karmakar53@gmail.com;alankiza@hotmail.com;kizalan@hotmail.com;anup@ufpl.in;kamran_saeedi@yahoo.com;alasarzamana@yahoo.com;jasmen_rl@yahoo.com;mr.kyawthu999@gmail.com;beernardnkrumah89@yahoo.com;naveed_gaill@yahoo.com;laengdh@gmail.com;bjmarwa92@gmail.com;nandodarthvader@gmail.com;cferlyanne@yahoo.com;sandeshsalunkhe001@yahoo.co.in;bhandari.1990@yahoo.com;clickit.in@gmail.com;Ahmedsabri@yahoo.com;Shamsul.online@gmail.com;Narintip69@hotmail.com;feyisoa@yahoo.com;kachasen@gmial.com;doaa3x@yahoo.com;seza1978@gemail.com;valdyzone@rocketmail.com;Alar.lange@unit4.com;jhor_taurus@hotmail.com;ahmadtarek32@hotmail.com;Developer@wirescience.com;fbenour@yahoo.fr;sissy_boboy@hotmail.com;wazeem.rock@yahoo.com;Naglaa.dewedar@yahoo.com;hind_h75@hotmail.com;Janeslowson@gmail.com;froy.2ramirez@gmail.com;liyemasecurity@gmail.com;tchella9@hotmail.fr;Hooksng@yahoo.com;21@example.com;usman696@yahoo.com;modi-mk-98@live.com;uddin.rayhan52@yahoo.com;mina_nabil9@yahoo.com;ulf.collin@hm.com;alcaesarabogamal89@hotmail.com;saade00@hotmail.com;alviansyahciicutt@gmai.com;binod.kumar674@gmail.com;zahretalyasamen-nadoush-love@hotmail.com;guillem1944@hotmail.com;narek.minassian@yandex.ru;ramesh.devycs@gmail.com;dennyyow_39@live.com;msalim.tawhid3@gmail.com;ahmad.r2011@yahoo.com;khan92142@gmail.com;prveen.dashing2007@gmail.com;shameersheriff100@gmail.com;girish@gmail.com;girish9188@gmail.com;mr.bombaztic_27@yahoo.com;woollyeggranch@aol.com;honken_kc99@hotmail.com;blueblabla@gmx.ch;muneebshahzad1990@yahoo.com;urcdmo@lmyhbx.com;16@example.com;15@example.com;13@example.com;12@example.com;333@example.com;33@example.com;this_is_a_test_email2@example.com;this_is_a_test_email@example.com;10@example.com;9@example.com;8@example.com;7@example.com;6@example.com;5@example.com;clyztfrd@yfqmnray.com;4@example.com;3@example.com;2@example.com;martinnissfolk@gmail.com;1@example.com;martinnissfolk@hotmail.com;francis.ellison@skype.com;jagflashback@hotmail.com;m.emostar@yahoo.com;drakod@live.com;harshal@snehapadma.com;ahnold@rocketmail.com;devicecontext@hotmail.com;kirbakalim@hotmail.com;lesovandrey@gmail.com;admin@oskol.us;stefan.ekholm@nj.se;ks@myweightdoctor.com;therealgrimbly@gmail.com;jr@aol.com;pjones0619@rogers.com;corneliuswilkins@hotmail.com;imran.haider@kabotintl.com;seun104@gmail.com;satish860@gmail.com;Andy.james@solidsoft.com;widowblade@yahoo.com;alashcraft@gmail.com;carlos26vz@hotmail.com;jeppe@cramon.dk;qazzysiw@uhjrnito.com;fabio@engenhariadesoftware.inf.br;david.bjorklund@gmail.com;ekacharoe@yahoo.co.uk;antonphamvanbinh@yahoo.com.vn;buzyjiang@gmail.com;felicia.holmstrom@gmail.com;super@lynhurtig.dk;err@email.com;o0h-15@hotmail.com;alexmo@gmail.com;meggiesp8@yahoo.com;jjb@12move.dk;kyle@massivehealth.com;test@test.com;steve.flitcroft@gmail.com;muarysol@hotmail.com;longnhi2908@gmail.com;hoang.tran@niteco.se;abi_ihsan@ymail.com;jdmcbride@iee.org;ma@amasis.de;roilaanish@gmail.com;silenceinthescream@hotmail.ca;alex_os@rogers.com;bbixler@deloitte.com;kamalsoltani@gmail.com;chronik@zworg.com;jad.gaspar@gmail.com;vikramsawariya@yahoo.co.in;onesisnunez@yahoo.com;ChaiFreakingTea@gmail.com;liudan_seven@sina.com;Eric.berntson@Snallerods.se;miloszkukla@o2.pl;admin@katehewittonline.co.uk;capgeminimobiletest@gmail.com;abcd1234@hotmail.com;leovpachon@gmail.com;tunamenu@gmail.com;choiceblack@gmail.com;llausas2001@gmail.com;puisto.kemisti@hotmail.com;anyspam2@gmail.com;slyman1011@hotmail.com;eglise150@aol.com;robert@devrex.se;tony.fox@berkeleycannon.com;dk@pensio.dk;gyroid@hotmail.ca;ricardo.wickel@bewotec.de;tfischer@tangible.de;mishkatahmed420@gmail.com;nayanahmed420@yahoo.com;abdoulienjie21@gmail.com;willweiss@me.com;wallick.tom@gmail.com;fredrik@reiback.se;peter16byrne@hotmail.com;asa.liljegren@thomascook.se;joakim.christopherson@thomascook.se;andlju@gmail.com;ola.gunnars@bahnhof.se;ben.hallam@live.com.au;mcnkbr@gmail.com";

            string[] emails = str.Split(';');

            foreach (string email in emails) {

                string downloadKey = GetUniqueDownloadKey();
                if (downloadKey == null) {
                    LogWriter.WriteLine("ERROR: Could not generate unique id, email import aborted");
                    return false;
                }
                Db.Transaction(() => {

                    Somebody s = new Somebody();
                    s.DownloadKey = downloadKey;
                    s.Email = email;
                });
            }
            return true;
        }

        /// <summary>
        /// Generate unique download key
        /// </summary>
        /// <returns>key otherwice null</returns>
        private static string GetUniqueDownloadKey() {
            string key;

            for (int i = 0; i < 50; i++) {
                key = DownloadID.GenerateNewUniqueDownloadKey();
                var result = Db.SlowSQL("SELECT o FROM Somebody o WHERE o.DownloadKey=?", key).First;
                if (result == null) {
                    return key;
                }
            }
            return null;
        }

    }
}
