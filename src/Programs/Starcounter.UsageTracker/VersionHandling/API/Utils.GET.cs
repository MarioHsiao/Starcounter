using System;
using Starcounter;
using Starcounter.Advanced;
using StarcounterApplicationWebSocket.VersionHandler.Model;
using Codeplex.Data;
using Starcounter.Applications.UsageTrackerApp.VersionHandler;
using StarcounterApplicationWebSocket.VersionHandler;
using System.IO;
using System.Collections.Generic;

namespace StarcounterApplicationWebSocket.API.Versions {
    internal class Utils {
        public static void BootStrap(ushort port) {

            //Handle.GET(port, "/reset", (Request request) => {

            //    Db.Transaction(() => {

            //        LogWriter.WriteLine(string.Format("Resetting database."));

            //        SqlResult<VersionSource> versionSources = Db.SlowSQL<VersionSource>("SELECT o FROM VersionSource o");

            //        foreach (VersionSource item in versionSources) {
            //            item.Delete();
            //        }

            //        SqlResult<VersionBuild> versionBuilds = Db.SlowSQL<VersionBuild>("SELECT o FROM VersionBuild o");
            //        foreach (VersionBuild item in versionBuilds) {
            //            item.Delete();
            //        }

            //        SqlResult<Somebody> sombodies = Db.SlowSQL<Somebody>("SELECT o FROM Somebody o");
            //        foreach (Somebody item in sombodies) {
            //            item.Delete();
            //        }

            //        // Reset settings
            //        VersionHandlerSettings settings = VersionHandlerApp.Settings;
            //        settings.Delete();
            //        VersionHandlerApp.Settings = VersionHandlerSettings.GetSettings();
            //        LogWriter.Init(VersionHandlerApp.Settings.LogFile);

            //    });

            //    if (AssureEmails() == false) {
            //        return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.InternalServerError, Body = "Could not generate unique id, email import aborted" };
            //    }

            //    return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.NoContent };

            //});


            Handle.GET(port, "/refresh", (Request request) => {

                LogWriter.WriteLine(string.Format("NOTICE: Refresh environment (database and files)."));

                VersionHandlerApp.Settings = VersionHandlerSettings.GetSettings();

                // Set log filename to logwriter
                LogWriter.Init(VersionHandlerApp.Settings.LogFile);

                SyncData.Start();

                VersionHandlerApp.UnpackWorker.Trigger();
                VersionHandlerApp.BuildkWorker.Trigger();
                return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.NoContent };

            });

            //Handle.GET(port, "/addusers", (Request request) => {
            //    AssureEmails();
            //    return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.NoContent };
            //});

            #region Hidden Area
            //Handle.GET(8585, "/hiddenarea/versions", (Request request) => {

            //    dynamic response = new DynamicJson();
            //    try {
            //        var result = Db.SlowSQL("SELECT o FROM VersionSource o WHERE o.BuildError=? ORDER BY o.Channel", false);

            //        response.versions = new object[] { };
            //        int i = 0;
            //        foreach (VersionSource item in result) {
            //            response.versions[i++] = new {
            //                version = item.Version,
            //                channel = item.Channel
            //            };
            //        }

            //        return new Response() { Body = response.ToString(), StatusCode = (ushort)System.Net.HttpStatusCode.OK };

            //    }
            //    catch (Exception e) {
            //        return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.InternalServerError, Body = e.ToString() };
            //    }

            //});


            //Handle.GET(8585, "/hiddenarea/{?}", (string version, Request request) => {

            //    try {


            //        VersionBuild build = VersionBuild.GetAvilableBuild(version);
            //        if (build == null) {
            //            string message = string.Format("The download is not available at the moment. Please try again later.");
            //            return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.ServiceUnavailable, Body = message };
            //        }

            //        byte[] fileBytes = File.ReadAllBytes(build.File);

            //        Db.Transaction(() => {
            //            build.DownloadDate = DateTime.UtcNow;
            //            build.IPAdress = request.GetClientIpAddress().ToString();
            //        });

            //        VersionHandlerApp.BuildkWorker.Trigger();

            //        LogWriter.WriteLine(string.Format("NOTICE: Sending (KEYLESS) version {0} to ip {1}", build.Version, request.GetClientIpAddress().ToString()));

            //        string fileName = Path.GetFileName(build.File);

            //        return new Response() { BodyBytes = fileBytes, Headers = "Content-Disposition: attachment; filename=\"" + fileName + "\"\r\n", StatusCode = (ushort)System.Net.HttpStatusCode.OK };

            //    }
            //    catch (Exception e) {
            //        return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.InternalServerError, Body = e.ToString() };
            //    }

            //});

            #endregion
        }


        /// <summary>
        /// Initilize the somebody table with users that got a key
        /// </summary>
        private static bool AssureEmails() {

            Console.WriteLine("NOTICE: AssureEmails.");

            var result = Db.SlowSQL("SELECT o FROM Somebody o").First;
            if (result != null) return true;

            // KEY,EMAIL;KEY,EMAIL;....
            string str = "PRETZUH9RHGQYSSXYDBHGMP7,excesiv@yahoo.com;FN4DJF5MNKYSC9YRSTV5H5VZ,LB@BL-Logic.dk;8YEQPS842NB5ZMKR4AP5URED,llnyxg@vkbxui.com;AHDFFEPUUH6XV6VYZ6F6ABLY,xixan98@live.com;66GEY2GRP89DNAEWPW6H5BVB,sir_cris2010@hotmail.com;HSX8P8UWYVCRZSHHCSP2TRES,h.peter.eriksson@gmail.com;E4WNCLSALGVD3KQQEZ5LURXQ,himanshu.singh24x7@gmail.com;H5S9387TMXXGUZEMC64MHJG5,gery2301@gmail.com;7GMDX3D4ST8KKHKH7JM88QE5,kartono.mgt@gmail.com;TMKUPFMRURSTZD5WHSFJLRA3,sivasakthivel.palani@accenture.com;95VDX7Z743Q2TSEF2PRVMVLD,elvuel@gmail.com;QU55CRZ6QRRTAXR5L58ET49Y,fisayoakintade@yahoo.com;WXY2ABPXU5M3L4F2P3EJNCHU,waterlilyart@gmail.com;DKKMAPD6CESM64ZD87SHWXL2,warship2000@gmail.com;E2ML253LTLTA5HEER855MV2G,marcin@nextgen.pl;CXCYG8TFQ6RNSFGM7HWVACHB,mekewijaya@yahoo.com;ABXVEVMH27W47YTSF28MSDUP,sajjad.raza@kse.com.pk;568W9B92FBCABWVV2WC2YKE5,henrywong94@hotmail.com;HLM6T4G42D8AUN839R4DVWPJ,raul_apaza@yahoo.com;LPN5M36232CGVPKNN7LJQ2DH,henry.lu@analyticservice.net;QR5SWUNSRWC9AY5TCQFMLU5R,lennard.cornelis@ing.nl;TLFFTR46HCED9LQH9FQK3TA8,okwuagwucne@yahoo.com;VNNDLHHSQQ5E6T8Z9HPJ3EDH,xoxoyouknowitxoxo@hotmail.com;QW75KXLNT2KFMF4HTXTFWZMD,purdypervert@gmail.com;RAB57LTTNA842SUG5AXHN9F2,geneellis@ctc.net;PVVWPHZTNCUCQYGAZQEQN262,son.doan@niteco.se;9FGL3KQDL9MTW2NGMLKY8UR9,spatacus1988@yahoo.com;VWWGHEZLFRL4TNB9PUTDZJ4F,dohoangtung@gmail.com;R3ZUMPYF3X479RGEXD38UGSD,teentitan1986@gmail.com;92DX8MBC375YG4DA8WFJQBDP,christianholmstrand@hotmail.com;XFBHC3CQWXZBWGR4RL2R3H76,contacto@lascarrascas.es;BZT7N24PLWSNX86G5KZYMRLB,moody.20082010@gmail.com;C7DF5684Z9PL8BU5M3M9HU2P,buyerfromus@Hotmail.com;NBCE9VEZL9YNXLKNXD9H2Y6Y,robert@uniqueue.se;ZGABSZ58YZ69Q74HJEG75X4R,tinawinston@valleytruckequipment.com;EGZWM5VYSXXSXBMU6W6WKRNC,info@iprog.nl;B2D8HUE58599ZF43XK4RWCZM,borja.andres@gmail.com;MG3F72D6UHCDW394KMHDF59Q,ramonnaka@gmail.com;VZDP9RBCJCCCRHY4WCFSBBZZ,raja.s474@gmaIl.com;VBF8APCBP33YGHFWW3ML25RD,s_e_s_m@yahoo.com;6A69GKBRWAHBEYTCE2F729ZE,lhbib95@gmail.com;T6NEKDCSMKM5ZTRNHFXDLMXG,normy273@hotmail.com;QU8RK9233DACNJ375KCZ4AB4,loulette999@live.fr;DA798ECKDVM9FYWW3L44LZZU,marcello.drewanz@wissens.com.br;WDE2TP2KYJWEQFYW5N5SKRDE,kwcqgn@hcwcob.com;7FAK34XPKFYKS68D3DD4ZJ35,viswanath.etikala@gvkbio.com;BSHZLB6NF9373ULDFB3F36ED,sairam1806@gmail.com;NBH6EX23EDSM43TMQTU6ETQA,uran3983@gmail.com;PM9V79V8E8W7SVT8JGN9JK44,Pablo@prsoft.com;BZXT9A49D9VP3YW4KFAPR3LQ,waytomyworld682@gmail.com;8S36R8SA3KFM6CGG3SR42S7U,jcho95@hotmail.com;F2LDJHZ5NC6JHZQ9VZN2AFPU,ola@sambergs.se;VUYBZ6U9B5GSMTQK4M7ULJU6,chettiars@dnbcb.com;FCNJ65WJY5TLWSWG7QL4NNG2,benedetto.mcdonald@gmail.com;YEEARNPY4TESC8LGYNMJ5KW5,tracy.bowman@crye-leike.com;C5XFJHHTTT8H4R2QPD39MJHA,ville.laitila@mingraph.com;A7ZQRY6U2YL8D3MBHZU5RFKJ,victor_augusto.gabriel@hotmail.com;YHKJKPH25GRBG53JZ43KALUZ,siva.chimmani@gmail.com;5FHS3TZERA6DLJSQ2QXKB47X,xmr088@gmail.com;JNH9SSMJS697HB7J38EBNBHE,ivan.bolcina@apida.si;DERTNSYDKTJTJXA5FHCBE7N8,hitechmca@gmail.com;8JQ3NMN2RREW5FWY4H4LC6AN,wesikc@knfycs.com;AX75JG5H6G2L5GQ7R4V5GEUJ,taylordubose@yahoo.com;AEDP5LQ5CVT8JNLTJ8JBV644,peter.derwa@selfservicecompany.com;KDYL64EF4GQ6892FJSHJYZ9K,obantanahal@2xprime.com;H8B9AYR497SQBTLKKLNUSVHV,skoyalkar@primetgi.com;2VRETG6MPUPYVLVPRW8ZKUPL,joel@loop54.se;DNYLGJRGZP8JR7V22L3D4YS7,kingkero@gmail.com;LNCKFRNWTPFEFE9BYKTCT58T,harish217@yahoo.com;LR2V7NDH8VF7J6A3ZJXLZ63Z,esmeralda.frank.castillo@gmail.com;A6LEYPUEJ29YPXUE2FDWNAE3,nigel.alfred@gmail.com;9NVSK8QYMGAMXBZTPZFQS7BU,abiiid@hotmail.com;EB6M9LD6ZPSCSJ9BT7C7Q7SL,kinijenika@gmail.com;E46ACDPAPCWWYUVGNTMG4GKN,wsf0001@auburn.edu;ZSVSQLAACZJ8KXBEPEFUQUN8,kqeulg@akqxmm.com;3TAMEY7D57ZZH5CNYY6E2ZLW,wxy2ab@sina.com;Y7FUFNFQYUUU9Y3AR6R52C3Z,joakim.wennergren@neovici.se;SMV7JJW5LTKR293PUVF3XTN9,lukasz.ciszak@hp.com;J5F2SLQFGAQSJBWL6ZH6PZEF,dyzhbb@bdtlup.com;FUM4KW338T7L3Q8HZE74SWEH,jude.rae@gmail.com;JJTQSWJ8EX9BYYCH76562Q35,johan.linnerhed@saabgroup.com;9NU97R4UH9Y349W2T74XAZE7,wallner.bg@gmail.com;DN2QGHQMDN9G6JYLAEA7L6YH,lhussey@stonebond.com;BYLDUUSXCLFPX5JYFMTD7PFD,cejchoj@gmail.com;53BMW53HQ6Y8K64VUBU4NLUS,hatsoykan@hotmail.com;WNYRBM9ALN7KWVYWVXQ8J95W,costa@halicea.com;7T2FFXRTPP5VQCQ82C57U5AV,emilian.ionascu@think2solve.com;VRUJU78WQPPGYP94VPMLZEXU,fahadcse07@gmail.com;B5FCVZJNNZJ4SP4FWZ8Y6PRD,adam@enigmaticflare.co.uk;CVXLKYCVQN7BKTPQW5TTZW88,val-imag@hotmail.com;7BZVXYUZLBD43KZATS349EML,geoffrey_forster@scientia.com;ZZ7PNHUR4DW6X3R6LRN8YQD5,mr@magoo.fr;CAQ24BTKD7SRMHD35ZNR67HX,ph@realtime.co.th;L4R38VQ2SSX6U3MQFQ8Z58WU,rb@selligent.com;ZDEW9KV8SCEDXZEZJ4PME3QL,804193721@qq.com;G2YDULTZKTVJGTY59KRGYBLW,happydvd@rediff.com;XGUU9N2GP4E8DG9TTM5FYRDU,bluebalas@hotmail.com;RHZ4P7UV2Q2EJ4L4LWT4J5HZ,flashkamilfia@ymail.com;H7D34E6KSFPZWDJBA295HY6P,info@4by4carhirekenya.com;JDFFBY2GHFM4JX8WGMR8YS4A,gtorresrodriguez@gmail.com;ENZM8LVWK9ZMC29WCTFWS34Q,gbanta@highspeed-solutions.net;EC6RCHSQQZBFJK8YAY5WCK59,hcullen01@gmail.com;SNWM6A3573TR8V3PGBGXVBQA,pradeep4405@gmail.com;M9PY2APRZCKJWPS4M7JJ8VY6,dear_angel0702@hotmail.com;T6D5S7UBUKBF8EEKYXL5WRV6,lfav2012@gmail.com;FQ22UGYZGHS6ADYWDDEP655G,djmzcin@yahoo.com;U68UUY9LGN7LFALWZWBMZWS2,Don.whittaker@bestbuy.com;KZPHKCJDU2EM2DWGNLZR84DP,johnf442@aol.com;SUXXEKUMX7PAY3UEXXAEA7MY,johne291@aol.com;AMKFSQZC5XGX9QG7WAN3UJC7,alex.meyergleaves@gmail.com;4MSUPWD78ZJ3BDEBF7VCHPMK,dcdebock@gmail.com;XQWPUDTJFS8Y26WA98PVC6DR,ashesh1708@gmail.com;PDD7ZXYUBDFU7W2QEYFTJ392,yuriaddict12345@aol.com;9KEU39JK2BW5X4ZXWHATM2PV,jingkun.lee@gmail.com;SX6ZQ5RGS6GVD34M394VTL5Q,luismontbau@gmail.com;HQVCMRAQW8ZRTTCJPVSMFTVS,riccardo.tarli@txtgroup.com;29VGTKV6AGMDWLM3U8S6GAZZ,starcounter@mailinator.com;PMZE7X5PLZTMC7YM7LYBHF3H,abergheim@lodestonesolutions.com.au;LUR6WUZ5M38TMP962PWD5Z96,peter@a2c.se;EJT8MVLZMKHPBHRZMUTE5JTZ,ssmart90@ymail.com;KFAZCV6JNFEE5UNW4VG7SYEH,jeremychild@gmail.com;YQG4YUFUKPUNUGQCX5WDN3LB,snezana.mitkova@yahoo.com;VMKA2P4X3RXNJ9TVJRH9KWVG,csavemark@hotmail.com;TT3QBGTDVWQL9LP57UEYCZHC,shurik@mail.zp.ua;KBDKDXAE9KRBVDL2VJP4R7F3,sebastien-cote@hotmail.com;Y7R98SZQ9FBWE2GGRPK54RGB,gre@feas.com;AAXNUNYXMJM846PF5WPZKW8X,dbfuzz@hotmail.com;53CMHVYMM64XX7GF794G62FD,devendrakatuke@rediffmail.com;WEBQTULLMWD6CM87G8MNANUF,tomnuen@agiper.com;6W89GG4LP4KLRLDCESN62EY5,johna190@aol.com;L2BX7XHTT45YRHZAM9ETSDDK,johng233@aol.com;HBKP7XN85KP96J57SELKGNWG,allan.udt@gmail.com;EY7VQLWY3Q2UU3KW8J42KPR5,peter.schuermans@selfservicecompany.com;UW6W4L337ATCQ7YSEAZMR7JW,serdardirican@gmail.com;TPLJ5XG3HCPH9UCH76EJFXDV,familyfun5@gmail.com;YWTFR6G2XFW92FQ3VNYFHMSG,Yy@yy.com;TD9PQWAT76XGQBAYXF8URSBK,patrick@videofy.me;HPPG3P3SXL8EDYBJBBJYU3ZQ,Mhaerojo@yahoo.com;MDH6TKLLXRVFMHNAAJPSQCXP,jazzyrainfire@gmail.com;QKG5CYD32QS8ZF3A9SWU8G59,johng23@aol.com;XMKNGYX5W8Y4KWDRAH9EBZXC,johna305@aol.com;MD8PTYSQWQGT47MFAUNN38EC,s.limonta@kline.it;Y8PJH44YD9FBAHGZUH992ZD5,bjorn@edgrenalden.se;7U5Q2L8YRH7QMVW28YGCWAC7,support@saba7host.com;F6Z8EW3TLHX4LCY95S9HUFC4,Takanosaga999@gmail.com;8KXYECZAPPL2MYZDBJ3EVSP8,lesbitcoil@gmail.com;BEZFJ4ZQD8JR9NQSM7GE2QFE,spadsau@hotmail.com;QNK9P8DGR6L2YGL687GMMQPC,ergun.ozyurt@joygame.com;L8KGSVRWAAEBKUCTZ6AHAJ8X,marisolsantizo27@yahoo.com;TBGDKGGQWESWREVFBSUVWW4Q,g.saldi@kline.it;3XFQYFL98CPJBFPLL9YBRRZN,rtsucf@xztmbt.com;H7XUXLFA2J3ERV46YA5REKMK,himanshu.solanki999@gmail.com;7GNXFXFE5EB4T9XFKUU3C35V,byusuk@gmx.de;MWPGY9WXSUKPRWMQHUTETN2C,umaribrahimawan@gmail.com;C9R8MKRF2WPNAF4Q3CEZBUV4,dave_cedric02@yahoo.com;8444EWCXBMMTG5FRKHWTPUYC,info@artanddesign.com.gr;CGPCWLJVCWNF587FG339WWRA,fleece@mail.ru;E2PEKKA6GPJNHXUSRSASVYG9,bjarne.christiansen@gmail.com;ZQQEJ7FGR5E53JX9V6YMHVC3,Venustark@gmail.com;7T67JY83M39BT44CTGEZLJ26,danielchapman221@gmail.com;U4FFAGK9HWH2NLB4KKCA5PWU,nikovv@gmail.com;UNZZ7PUAD759HATGLE2JJHWT,darren_maglalang@yahoo.com;8MJZJQRR7GXD7PBUHJWYFUWE,nuliknol@gmail.com;VFFM7ZY4D6TRPS88NFRKRNXX,guitar_junkie_0912@yahoo.com;Y8KHYVYX9PV7544FH5ZZNASD,lovely_s12@yahoo.com;Z7ZMSYZQ4WZ3RU5AZSY3DNZV,mxqueerco@gmail.com;6SAAR2LU897ZUF3ZLQ5PLBDS,jugqoh@qxvgyj.com;P85CUJTQ9UEEGF8SLRHH5HPB,canbenegative@gmail.com;LXZR8NVWLHQ4DNMZ62N3YLN6,magnus@xsockets.net;N5Y7SXJZSMZNAUNJ693T7VEK,samdei73@yahoo.com;74Z6STBZCEPLW7WSAFSL2Z4N,sudhakar@fabergent.com;CV7VEHCC9GQCS3EZBNYWU267,bb@millermalta.com;32PB5VHQUB3VRJ8F56HX5DEK,uzi_hiers@yahoo.co.uk;29TBU5KRGCTVMYHB7P2BZQRA,bhunter@innovasmarttechnologies.com;LPFSP92YS2BWCNWN9FV45SYM,megumi_dwc@hotmail.com;FKL9ZKAXV7DVRQDDABCH4F2P,aju_aju88@hotmail.com;X3RXXZGKDZRM45PVQA37UW64,dr0zaxx@gmail.com;5S9Q2A9ZATXXY3BJD9Y86RFM,bstearns@globalfinancialdata.com;YXGPWVVVUBJN6ZJBXSM6WWAN,www.sindhushankar@gmail.com;6H8UX7P8657K57S64PNKDXU9,donyinaeric50@yahoo.com;XSD6Q49D8E7BYZB9TJPBTNAM,SRHINGE1082@GMAIL.COM;B6PASDC5YZE8CPJBUFRABHCN,jribeirosilva80@gmail.com;ENK77ZY3V22CWLL29XQFNS99,test3@test5.com;Y6M3EAWAMKQ6QTDGPKZ8VMVW,asa.holmstrom@starcounter.com;5G8JQN7CR87QDSWJYF76ADAY,test3@test4.com;8FPGWSPGGCHMPERSB7BRX899,Nniizzaa03@gmail.com;97DUYEJ4V4SP3NYWLSFECRQ4,mavullayya@gmail.com;FGUHTHSHM74V9447HCQEXULN,everettsh1@gmail.com;BD8RKWNE3GVKM9558Q6L4WBL,sathyaco2000@gmail.com;KP29E9UDY78JNC36SAJ2U475,mit.deepan@gmail.com;NUSCJYKW3HB8SNHZJKCTKZ3E,sjpzu@vodamail.co.za;74YTYRBSP3GSNXAUQM8KZBCW,myatkyawthu85@gmail.com;BW4LU5S5NLG9MX8UBFK7VBSE,jani_26@abv.bg;BJZBYCUKSUSZB3K6A8CPAZYX,habiburkhan98@yahoo.com;VQGBYBYLRRCQEBBVY9TWZXS5,ddd@ddd.com;LJ5YJVXD5H4HHFS7XGDAKNAU,ccc@ccc.com;GYAPHU2XFD4NAFPRDPMEPQCL,bbb@bbb.com;8VMLR5PAQ7PRP8NF5B3TJGPQ,aaa@aaa.com;4QJ4D4AJFY388MF528M45XFQ,jckodel@gmail.com;HEJGFWNR3A72RNXMPFNTFKR4,gibbij@gmail.com;LHV5J9NQEPDLHNWZJ9MK56XC,aaaa@aaa.com;WALSWME8W2UNXLUFNE6Y65DF,alaaabdelatif1961@msn.com;9ERZVC9648874GYG8ZY82L24,omkingsanctuary9@gmail.com;GGPGCR2NNUDA58W9KWAJESJS,sarazamora_24@hotmail.com;NQS3EV4V2Y4EEYRN2WLDZBGK,wayne.vos@gmail.com;VZAQFJLMF98FB4LZMQBUU78Z,Hanna.Nilsson@societyobjects.com;DPSWX4KFSL3WH6TYYCN4AQZA,niklas@societyobjects.com;J9TWZVVTR9A6LCFEZJ3DJSC4,niklas.bjorkman@societyobjects.com;C59QAFMA8EDHKQ7GRP78TJCV,iliasushaibu@yahoo.com;UKKN43SNZPVNNBZ3UDHW2EXY,kartikay2796@gmail.com;NZMRMYK6FCWTPPMKXQC2WZ6W,katikay2793@gmaol.com;44GCNZK5B2MRVSJTHCVEXJRU,jimmontross@gmail.com;6TM7DWG5ZJZ6DF66266PXA3N,baresaidoo@gmail.com;LTDTZFJFSGHD294RDA3ZSMH5,nopijambak@yahoo.com;Z34TEF8NXAXNVG68NZNXB2UG,per.jorlind@me.com;RTJYM82ZSHJA89K5GEYGLNFW,cbr@appearnetworks.com;4YH9CKX8Z99QGQ2YJE8UV9V6,yd0odd@yahoo.com;6EYJ6ERRL8CR6AX4BYXXVBM2,mallesh.santu@gmail.com;2FAJS8NSAQY9XUL7B59N97WK,jitamitra.giet@gmail.com;KC97CWYU7BFC8A6ZFU4M8JQH,natalia.szymacha@gmail.com;V2LSSWUUYWV7F28GUVQQSLV7,dcm-starcounter@capital-ideas.com;VHUD9YCW96VDSB696RW9SFAG,gabisreal@yahoo.com;QTF3EAAZHTC66A9LSN58Z9SV,archanad83@rocketmail.com;TWR8ESD6YH8ECJYTJ5LMGBS8,kumar.4gid@gmail.com;547AFV332GSMGGFGYTDVTR75,kirtisiwach@gmail.com;LP5WW5TY8JUX68H2KBGMCZJ5,jsfnhngnnshaanika178@gmail.com;KMTGN8748FQ6T89759C79TXK,vishalbhormum@countryclub.com;C8P88EDMMCQPXSG2ZD8X6REF,yassinar_64@yahoo.com;WNQYYBN8FH6T6XC6GQ2C8BSH,excursion.hemant@gmail.com;W4ASUSF6LC4ATYFR48VBV7SL,jhal91@yahoo.com;G89DCN5MZUJWD9ZR2DY8CQW9,eldani688@yahoo.co.id;66DGZBZZ85699WCTALF3ZRB7,chadani@sglg.asia.lk;C7YA8DWXL4HCR8B7STWQTKHA,don.cornforth@acgsystem.com;85RQPRLRQRSHP98E8X7UHTLM,don.cornforth@gmail.com;6SBDSDFV8WACG7L4UEMC9EJ3,rondro06@yahoo.fr;E28QHE7TDKCD4N4DA4K7WPUM,noli_inter@hotmail.com;U4L7Z9VG4VJRKUEVFNZ7CT3P,SRIMARYANI21@ROCKETMAIL.COM;FVTCKAQV4N2BERB8EXLY9TL2,rahmat_kodja@yahoo.com;35M3ADNVZ4TVGFKJ3JA6TWVL,mohmed_f322@yahoo.com;Y5LXFF89T772M8AFZGSP3P9W,www.yogaharjito@yahoo.co.id;DCX8SYA8UHFHE4T4FC3PDD64,innersakina@gmail.com;V2HSXSYT6QLN4M6UWZSU7N8F,vinay@revinate.com;8J2BTGUQ7255Y2Z54P8K55VT,yanrkits@gmail.com;TFQWNEZWRNEK247JUUGRZVFQ,hakimsalim4@gmail.com;CUZFJVFFQMR3EE5KFCYSEY8Z,ad66@ukr.net;HZYXYPBKR3ERVTHDAQERY5ZH,babyqurllovex3@aol.com;NWY9SWNWZNHDDW3VDTQRP7M4,Dynakamalia@yahoo.com;JCDXACNK3Z4STNUB9MME4QL7,d.nerada@shaw.ca;38GBXDMDJM2NCDWPRS4DSE2A,kmreddy_lichfl@rediffmail.com;RYFBZYVGNU698G4B5N9KYV9S,subhash123456789smailbox@rediffmail.com;SNF9ECDXTXDK7XBCCUKEK3B5,mun_bali@yahoo.com;TMUHVXP2FKK6PYP5EMA6EEW8,silichungweekly@gmail.com;ZVBTR6PJG6J39N37JVLLK4F4,waldyhester@gmail.com;AJMV3S9LSSMPSLVRNSR564NK,eman_super@yahoo.com;CJV25C8NZ7ML4XG2WQVL75J5,brainwaves01@hotmail.com;5G6JYE6X4VM9M73D2MZ7SBUM,nurjahan.nesa@yahoo.com;7J7MK7683PNZTBCHLT3T5X8B,anilpatel.ladol@gmail.com;8L23MGEFM7WWMYETHRUNJ7RM,mal873999@live.com;5JSCP3RE9UKCWR29VQ3WKYKD,weix77@gmail.com;HJJ286RAH2MPUKVKUHN7FTCP,vivi.anne@windowslive.com;2ZD2VA94SGJAPMCBNCDNDL35,frel8817@gmail.com;JCAQB2K6TGVN2RD89PFRUC5J,frel8817@gmail.com;Q2FE9CR2CZ5N84VAVJ57S29M,frel8817@gmail.com;BYCNQX3U5MTBUXCJDJYTVWSL,frel8817@gmail.com;5VFR7J7H4VYDYBTQL5ZH629P,frel8817@gmail.com;X4HZ53WB7WF4N6CJUS2M8AJ8,frel8817@gmail.com;M6SZN444QTYEKBMK23EGYYN3,frel8817@gmail.com;QKHRB2QCFGDR59YGVYBHP6K8,26@example.com;3U99SUCFYB2SX5YTVS8FXLB3,prarom2011@hotmail.com;5U22YCC2M58DV6VUKHV77TS8,25@example.com;G9CBTN67CNVGR3QBR72HK642,kostis.malkogiannis@starcounter.com;2HNBPL9Z3LLFS9LYHSEYNYTR,kostis@starcounter2.com;8Y5KHP99NW27A57NFP4TDAM9,jadeebabes@hotmail.com;FQJXZ7VPXCMQC9LUV2ZR6VEA,kamis2829.KHM@gmail.com;6PCF2BACTLWSEREMBJT857RH,dbs101dbs@gmail.com;KETGXC7X5D7DRX6GQVB7DAF9,arielram@gmail.com;LZH4NUU5ZZSKVJ6VTYDTDHQ3,headline1118@hotmail.com;6EW296W2CFG2EKUBA2FG6X3D,Xcunamix@gmail.com;HDME9A5Z6VTEBA8C4ZF2PYQT,kostasdre@yahoo.com;G8KVBFBD84PZBQ4V55BAHY5Y,michaelayers@yahoo.com;F9ZP3L3K9NNZEEGA3L8PYMAR,v20jeanpiero20@hotmail.com;48HWLPLBQPKQE3ASRPLMXYCP,Masud787mbs@yahoo.com;KPJGUPRXLEVXXN34W4DBDA4E,vivimusi@gmail.com;FAB75D8UKDJY8C7SRNMPQE52,umin.msmedia@gmail.com;L8WVRB8VSMHQM6QSSLGQBRGF,Abanob.boney@yahoo.com;Y4DCMRMW25Z3FF6X76JW3FAL,jan.berggren@neovici.se;XY4VZFWHDJEQGCHA7M8494PH,regentblogs@gmail.com;DMLSKX5UT3CXFBHWLNYBUTJ6,lillyinfinfinito@gmail.com;QV6GJ46HZM3Y9UPRHTDUT5W3,hour_hour16872@yahoo.com;SUG85TLAZ6D62J9A6ZSCKV57,Chou_d500@yahoo.fr;8APKTEMFN6TNFSKZ6EVYYR63,ebruli93@gmail.com;DSP3S8M9YMSAQ3LJ4QJE8GYT,Sreylenkhath@yahoo.com;SWD5Z22J3T2YGSQE78NBZG8P,tareqsaid19@hotmail.com;9YCRUVEBY7DRWEPV78HTTGHG,santi_espi85@hotmail.com;FF3W2TJ3SY469Y3DAP6GH3G7,W002012@hotmail.com;2VSCWTFCDA54H5EZ4GCDW2C8,Kadek1985@yahoo.co.id;4JR8FMHSPSCW2Y7Z7QA37NUD,sudhir.bhande@yahoo.com;2FLJTNE4RD2NVDBD7C9AJEZ6,johan.bergens@gmail.com;KFRKWA59PC45XHZMEAW54ZCN,Franciscoalmeida1986@hotmail.com;6CQFKB62TRYZCY64NM8LG5GY,wufong22@hotmail.com;8XSVTQDW95EC7G2HH5AR98CK,suwat_wat1980@hotmail.com;53TTXJLS782YLEVNJN9C4D56,perezmichael17@aol.com;94NEGZVLJXAEAZRYYYEJCDUF,Yinetjaquez032@hotmail.com;64GPTX5K4QPJRJ6VS6KFV3B3,sayyadrizwanali@yahoo.co.in;MZ22YJ2DTQ7MBZB4KB5GKGNK,communitydevelopment46@gmail.com;VF7GRA5DRNJDUFPDQ3LEW3WY,popolay629@gmail.com;W77GSAPGYQBSY7KCVRJEMUN6,manvendra.patel1@gmail.com;YNPLLRKCACKL66UEM223LXAC,blackbutterfly.p95@gmail.com;US88EC7YNTLEJWPR5KLEWLQS,jess_041581@yahoo.com;JLHZTCYPRZQC8RX3YVVV2J48,a_allam100@hotmail.com;A42KWLWR6GGZU4H8UNEFX9L9,mohamed.love88@yahoo.com;T4ZX23YX46VPJD4479EXWZDP,arshadbhutta@rockemail.com;Q24R873EKBFAV2ZK2YUC5CS7,martenc@gmail.com;WHTQ9A7YZ9E8FNWE277GPYFX,soowaijian@hotmail.com;FT88GPB49Z54TD687QGETTRN,tinio2net@yahoo.com;JC4LNY3CVJH59B6YWZTZSH9D,asd@yhaoo.com;3K9ZNJPJLHFNLMT9AJTYCTTU,raaj_001@yahoo.co.uk;9WQPSBNR7QCJY994G2T23BWE,biltu.karmakar53@gmail.com;94YA5QKVACUL7W582SRVL94C,alankiza@hotmail.com;TUFU2T5R5HU3ZV6ANKR92MPY,kizalan@hotmail.com;SNKW9TP9NV2MH35ZNNSZJQHM,anup@ufpl.in;YE4HWXJPRAA8NJ9FB48ECMDF,kamran_saeedi@yahoo.com;BJ4W5Z9BN7LCPZX65L9X92LQ,alasarzamana@yahoo.com;5LVDERSX4VM33YQM9MEWA8PM,jasmen_rl@yahoo.com;QLR36ZE82U6GSETB6E3ACW8S,mr.kyawthu999@gmail.com;XDMHNE8RLS9ZC55R84EZMG8R,beernardnkrumah89@yahoo.com;F4H7X4XTTAM35AGR6JWCVJFR,naveed_gaill@yahoo.com;J5UWYCM6AWAM9S53BHPXKY35,laengdh@gmail.com;ZG6ZWNWMBLXEJ9WEWWKYZL3D,bjmarwa92@gmail.com;YPGVTFBYXPQQLMR9P3HVVWRP,nandodarthvader@gmail.com;KXRTQAHSXD98KQA8WRGPQZLX,cferlyanne@yahoo.com;H5KSP6WLGUFHKQS9Y72CNTJU,sandeshsalunkhe001@yahoo.co.in;JPBZ3R27B4F8X598BQA36T4T,bhandari.1990@yahoo.com;Y933SU3RT8HS2GMHQB78KXBH,clickit.in@gmail.com;VK3GJ74U8CVTT6QE4B4LW2AH,Ahmedsabri@yahoo.com;W7TSTTX2DHHCYR2XGJMDLK3Y,Shamsul.online@gmail.com;4LKCE5W2NXJJFGK8NRJE46KE,Narintip69@hotmail.com;P5F5A5EQER2M3BWLK9QK6TWA,feyisoa@yahoo.com;G9BJK9ZTG6LT7L6GHCTQTVRB,kachasen@gmial.com;6KC9BCRNJHKLLLWXB4HPVHM8,doaa3x@yahoo.com;FX78PU2ZGLF3TX3VKTNSY59D,seza1978@gemail.com;2HMMW7RQPBM4FQUB2KX9E6J3,valdyzone@rocketmail.com;W5LZ6NX32NMQBSMGDBTNJQB3,Alar.lange@unit4.com;YC9FCKNQFW8D8XVES3G9UG38,jhor_taurus@hotmail.com;K6RZWUTUTYAX3PB6S6MAS3LG,ahmadtarek32@hotmail.com;4VA2DCUUKW68D3EUTBEV7QPU,Developer@wirescience.com;Q5AUK4PSEZ6UFF79CH5CRT69,fbenour@yahoo.fr;3MKLGLJJ33APSBY7KYLBNJY3,sissy_boboy@hotmail.com;D4V6F4MXZPYENPFQAWH9KZQP,wazeem.rock@yahoo.com;TWFDF753PPD2KDVRNB9MPT9K,Naglaa.dewedar@yahoo.com;2XTNZFU49K7Y4CC2TP5MEMHM,hind_h75@hotmail.com;MP5Q9LZ4KDUHCVZEP78AAANS,Janeslowson@gmail.com;X7UG4CDAYWN2TTYTYNJU795H,froy.2ramirez@gmail.com;KKVP2AK7LAN3D6E23NEC842L,liyemasecurity@gmail.com;GNYRU58VXJ57D9ZDQRZASJPC,tchella9@hotmail.fr;VYVZW3ZLNB87YJ5FRWBNLW5N,Hooksng@yahoo.com;D86JU5AN9CEB4HLAY92RN3FH,21@example.com;H95YHV9HM3QKXCCNARD3DNVK,usman696@yahoo.com;FAFK4CHCD3CX6ETL6WHE56GN,modi-mk-98@live.com;V2SFDRYDS5H3CAZ84B8R32CZ,uddin.rayhan52@yahoo.com;LFMB7L7F2X8R2AR8QY2T7M24,mina_nabil9@yahoo.com;PD8ZS8E8RFNJKJJDJX8TC8C8,ulf.collin@hm.com;HRHBA353J2W5K82P8N8WH95K,alcaesarabogamal89@hotmail.com;KF5D5566U8D4QJDH62UWRJGS,saade00@hotmail.com;RYV4SJW7966ANB2F7EH97DNZ,alviansyahciicutt@gmai.com;JNR7WY5R237KXBZ37DX4U2L5,binod.kumar674@gmail.com;6G9GDUQQACQ962H8F5XYGE43,zahretalyasamen-nadoush-love@hotmail.com;AVEGD67KGC9DDPQHQQQRG8JU,guillem1944@hotmail.com;C7AAQLS8WT4BFKBXMQ5Z6L6K,narek.minassian@yandex.ru;HTLZ28Q67FKGV7K9ZVX79MUF,ramesh.devycs@gmail.com;WLTKKD4DRGEHPSJ4MTS4H4BS,dennyyow_39@live.com;N3KYZPZXHX66Y5MDW67BK2TQ,msalim.tawhid3@gmail.com;QQCE7EEQW6MUYT2S76YL8DK6,ahmad.r2011@yahoo.com;5BMF4VC2NJJEVXFE9ZENHQYP,khan92142@gmail.com;22B2TQ62UA2X6FZE4KTWVB7J,prveen.dashing2007@gmail.com;TFW2YHEK9WLQ8TYMFVA3XCAB,shameersheriff100@gmail.com;ZPGHM7MH8PQV9RC4U54STJCV,girish@gmail.com;KZEX9D66JZA687HBWLK5SEFM,girish9188@gmail.com;NZWC3WVKZ8W23U3Z8P8YSFYV,mr.bombaztic_27@yahoo.com;3AJTEQZ6753LMW8THGNJDY5Y,woollyeggranch@aol.com;AXFZR7DTFCUXD5PFAHBBK5NV,honken_kc99@hotmail.com;7UZ3YHBM9C7DA5BUJQD49U83,blueblabla@gmx.ch;REKJ4JVJBVZ4U3EM83CSS2QT,muneebshahzad1990@yahoo.com;4P4UB3RZDRST4RCKVLNQP5HG,urcdmo@lmyhbx.com;SGLTWZBZ3F7XFTFVAF4WAHC8,16@example.com;CQHC4EGF8Q27SGPQXEHMUJZM,15@example.com;M4XUTLUU8LDN348R6GNDRSVB,13@example.com;LB54HAXBRDMR95LGMS2GN8YV,12@example.com;67RULTFYZZ8X4FXTMPE8GRNZ,333@example.com;4JKMSS68BSQDBEK44GSQY3NN,33@example.com;3C29ASZWHPT2XXMWWVKCZKNU,this_is_a_test_email2@example.com;4EHR53Q8DCXXRX9CE9EFCDGF,this_is_a_test_email@example.com;B9TLDUMR4ZJ7TH5P459UB3WQ,10@example.com;YWSUG968XXA4VWWVZKG4AUZS,9@example.com;P2QJR4HVER89EDGARVW7TDV3,8@example.com;G6TQM4Y3QLGHJT2VZJ3AF3GX,7@example.com;DZDZH5T2ZA2H9NXQSL9V76SX,6@example.com;JWV5RJFZMF8TH94CCB8HHXGJ,5@example.com;EXEP9KUCDWLDX852E7MYZLT3,clyztfrd@yfqmnray.com;VW4PV3435QPWDPK29BHKG2JT,4@example.com;EAX2U3984RR5YGRS5Y2QQVCN,3@example.com;ZSE7UN9GT2ZWRSXXNNAHURYV,2@example.com;HUZMUHTY4XT9WXPUBYGFTTJF,martinnissfolk@gmail.com;TXLYVMSYJE24FGL3VFJU74AF,1@example.com;K5GBEFMBMFYASM3KJDKR5ZD5,martinnissfolk@hotmail.com;TZN9VR7MXSGSVY45FVPK2US2,francis.ellison@skype.com;7QZCKRZ5LEF2A2FG7VTGGQG5,jagflashback@hotmail.com;R2MUPM4RW5NTXDEVWHSEAREX,m.emostar@yahoo.com;NQCZ7D74MMJBZS3C8FZNPJBC,drakod@live.com;3X9NJT842S5KW5Y5P3WJ2CUK,harshal@snehapadma.com;85CMYUTSMDBW2QPBZL8TMBD7,ahnold@rocketmail.com;2MD4R4GY2K9K6XMCYDSKDRTT,devicecontext@hotmail.com;JZPSV6JYTKD3UZM6WBPU9TYC,kirbakalim@hotmail.com;FJ98J3GDXFJBL5KT5MVQHMA3,lesovandrey@gmail.com;K25VXNNP67Q4TSGTRCD5M779,admin@oskol.us;LFEFKAC7B5T8ZXMQMZ8GVDJ4,stefan.ekholm@nj.se;AV9CVSARTST8DPVSHYCK4M2W,ks@myweightdoctor.com;Z6PR3B5F3ERZJG7NMD88BV2N,therealgrimbly@gmail.com;JV9VNC8FCNGQLU3TX3QGE5BQ,jr@aol.com;CSKJ37H3784ZVWAU3NZ6EEDR,pjones0619@rogers.com;RMET32AA2VNF3RU4PU5GQHFV,corneliuswilkins@hotmail.com;8XVWSZFUJ87CEBML729ZEEYH,imran.haider@kabotintl.com;6MMQLU7X3D6WFUNEBYXQYF32,seun104@gmail.com;GBSJU89VB2BP675AC93FWCAV,satish860@gmail.com;847M3NRYYVNQADHE9UH9KANJ,Andy.james@solidsoft.com;XFXMNFVENPSP2SL24PV7UHBA,widowblade@yahoo.com;M6NJLEEB55MNU5AVX5PV2DBV,alashcraft@gmail.com;GHRZKN5QUX3XUWJ34LTXJ573,carlos26vz@hotmail.com;2YFZDZAEXTNFA5NCMZT4P7LM,jeppe@cramon.dk;3YH7BX9RVTUMSULG4XZQEYUC,qazzysiw@uhjrnito.com;K3L3FNSDMV6YTQSMPHKCR87M,fabio@engenhariadesoftware.inf.br;QWEEWPD8F4GQZTLQ9XBCJCUW,david.bjorklund@gmail.com;DXWJZKQ695QZPJP4KRK5AD2G,ekacharoe@yahoo.co.uk;R5PHSYP2EX8RPJTTY829HMCP,antonphamvanbinh@yahoo.com.vn;AS225JL2XXBQKNEVQ3PVW9MN,buzyjiang@gmail.com;7XE56SK5UR89MHRRSCWZA55W,felicia.holmstrom@gmail.com;DJN6VRT73M65FJUZCJXMKWH3,super@lynhurtig.dk;DX6W6YTA73GWV3VVRM2PNGNS,err@email.com;3EZNKCSDDUL3SGN2EBZFD7PM,o0h-15@hotmail.com;ASVHTVB93XE8EJQHRQUAY9V6,alexmo@gmail.com;F354Y2EB5KKKAXEUEAN25MJB,meggiesp8@yahoo.com;MGEA3PNX4SUECJ7ERDPNHW67,jjb@12move.dk;GHVH4GR4BV8VWGC4VGNBLSGE,kyle@massivehealth.com;R865SAKLB8C7NS3ZG5CN3TEC,test@test.com;M2RSQHZLEY2KQRQVNXBGF3TL,steve.flitcroft@gmail.com;MBB8TGHNZPTBQ4RAH652KCQG,muarysol@hotmail.com;GN27ECLNYXVYY7FQB38SBSLQ,longnhi2908@gmail.com;LFBNXGRXJ6Y4AMQUBDS6MANM,hoang.tran@niteco.se;2JXT475772FH9ZK76EVJTUJV,abi_ihsan@ymail.com;FWEK7B3Z3MYBNATHR58GAX6G,jdmcbride@iee.org;ELLWPL7SQUY3F42N7SKLA6TM,ma@amasis.de;DL463E37UQSJDVDQGGEK8VC8,roilaanish@gmail.com;U67FB8HR97CBWV879N3THSWA,silenceinthescream@hotmail.ca;2RX3F5YMQTE9C6SKEJCFWD2D,alex_os@rogers.com;N99D7FWW3FNQWVZHTV5GCUD4,bbixler@deloitte.com;YX7ULESA5G7397R38ZZKXMCC,kamalsoltani@gmail.com;EE9G2N8BUWS7W6ZLMDRF8EAE,chronik@zworg.com;DEZYJ9SJKVETV524A7H9AF4F,jad.gaspar@gmail.com;3AGMKVK5WPN3Q74ZKXUQGY8M,vikramsawariya@yahoo.co.in;56R24R53ETNKJ269L34TM9E6,onesisnunez@yahoo.com;37T8JJGNVA7Y3NM9N3PH32X3,ChaiFreakingTea@gmail.com;EHZSSNCHFLS87C4XVQ3KAK3Y,liudan_seven@sina.com;6ZKM6RN4P2HXQP2L2FWCD2TZ,Eric.berntson@Snallerods.se;V47SKUVGR7CKCXPAJGGFLLK6,miloszkukla@o2.pl;VRXBTXNNQF48WDFSJ7T7DXMK,admin@katehewittonline.co.uk;853CUMUWCTSSCVW5BYRPDQZS,capgeminimobiletest@gmail.com;9P65259Z5J48L74MCCM4RJ53,abcd1234@hotmail.com;B8D5P2AT6DUP7JEBAWBLQ2UG,leovpachon@gmail.com;JUHKTJQ642SRJBG7K22YWJRZ,tunamenu@gmail.com;NP52AKFJXSDJCX5A4NV8YK3B,choiceblack@gmail.com;8SKSB2679BFFMB7EPJZMP9KD,llausas2001@gmail.com;PHX5NQXT6B6UEHS8FD3QL2GJ,puisto.kemisti@hotmail.com;KNJ4SAPFADKCQUHHCKK7G7WK,anyspam2@gmail.com;3ERM5TK3RU663TSCBZ3JWJXF,slyman1011@hotmail.com;WKFWELS6YH57G5BXJFWZJ3Z4,eglise150@aol.com;TS7QG5BWX3WNSDAN6VMWFS82,robert@devrex.se;LZZQC9EMCTFFANSGGY4K72G7,tony.fox@berkeleycannon.com;MUMBQPABA2VTZSE9SWZPBGEB,dk@pensio.dk;LEY33JCRHLCPNMDVP7PT7FBZ,gyroid@hotmail.ca;926NUJW3XPJU8QHYVXTRATM2,ricardo.wickel@bewotec.de;CZZLYBLRVUXDWGPVN7UB9R5D,tfischer@tangible.de;YAMBQ2Y7GCM38WL7JNC5D89S,mishkatahmed420@gmail.com;8C6YJSJMHGP6WFSYWQFFYBK5,nayanahmed420@yahoo.com;RVM8K8DJWEC33369AMZ536Q5,abdoulienjie21@gmail.com;NN98TEBJJW9DJ2VXAU8QMY9F,willweiss@me.com;QKRET6TB5VXECJVMS8GZQHEH,wallick.tom@gmail.com;DLJ4MPPUN3ABP5NEM67CQMCF,fredrik@reiback.se;UYGJ8G2CGF8FKM4SUBJ93WD8,peter16byrne@hotmail.com;U8UFLQQ9BFGKGX4APFZG4KKQ,asa.liljegren@thomascook.se;7HFGEWNG3E3JSTM8RT532M8S,joakim.christopherson@thomascook.se;64FC8FKJRFE7NWDVU2LTS94W,andlju@gmail.com;BZKY36U23YJTZ32KE6X7X65M,ola.gunnars@bahnhof.se;G6S7ELWHCELAS5HV5P3HMZL4,ben.hallam@live.com.au;A77AJCGHCNN7FXAW5KRK2UE4,mcnkbr@gmail.com;ZFRWVZH38SL7B9LPWPRRDYGM,marcin@nextgen.pl;JCEDJTFHVXHZLUSWC3B79SAZ,niklas.bjorkman@starcounter.com;PPVJ8W4VFXVKRR57AR5UPDJN,joachimwester@me.com;ZC9QC57RF33UW6QFEN5W3H7G,christian.holmstrand@starcounter.com;AZACNXTVVVGMR9GJH8CJ9DBK,per@starcounter.com;ZN7YSW8W69HFGQQCGAXVK57U,Ruslan.Fomkin@starcounter.com;4FGNLTJWY5NYAEJ3KC44B3MF,anders.wahlgren@societyobjects.com;NCZ6LA8QNA9KKX9KZGD2H6QB,jungkun.lee@gmail.com;6UFGLERHKZBUTY4Q68PM733Y,gutlapalli.kv@hotmail.com;A5VSFMTZC7JUJUMHR25EHEM9,sollie2112@gmail.com;F7ZLSLXDTDPJR9QPYCA63HWZ,jay@jayonsoftware.com;5NMCEKN3KJ8U36M58KBNA6BP,tibuchivn@gmail.com;K4LA5AGXNL4PJ27H43ZV6UWW,brian.madsen@outlook.com;QJGLAWM4KVMKKH5HC8ZX358D,qader@37.com;CQDVGSA8FU89G33RUP8BKY6A,kmcdaniel@kamtel.net;2UZQ9GDQBN9FY69PHEGLBBGH,deepa.palaniswamy@sryas.com;SNZVPTQYUF5ZD557QQZDWK3U,terrachan@live.com;9QZ6H24JDBN2PYSDT4SRWUAA,torbjorn.sjogren@ffcg.se;AG3JZR9ZY4PJQEDCQMSSU6RB,himanshu.kautish@hotmail.com";
            int newUsers_cnt = 0;

            string[] items = str.Split(';');
            Db.Transaction(() => {
                foreach (string item in items) {
                    string[] data = item.Split(',');

                    Somebody dupcheck = Db.SlowSQL<Somebody>("SELECT o FROM Somebody o WHERE o.Email=?", data[1]).First;
                    if (dupcheck != null) {
                        // Duplicate, skipp to next
                        continue;
                    }

                    Somebody s = new Somebody();
                    s.DownloadKey = data[0];
                    s.Email = data[1];
                    newUsers_cnt++;
                }
            });

            Console.WriteLine(string.Format("NOTICE: {0} users added to database", newUsers_cnt));

            //string str = "excesiv@yahoo.com;LB@BL-Logic.dk;llnyxg@vkbxui.com;xixan98@live.com;sir_cris2010@hotmail.com;h.peter.eriksson@gmail.com;himanshu.singh24x7@gmail.com;gery2301@gmail.com;kartono.mgt@gmail.com;sivasakthivel.palani@accenture.com;elvuel@gmail.com;fisayoakintade@yahoo.com;waterlilyart@gmail.com;warship2000@gmail.com;marcin@nextgen.pl;mekewijaya@yahoo.com;sajjad.raza@kse.com.pk;henrywong94@hotmail.com;raul_apaza@yahoo.com;henry.lu@analyticservice.net;lennard.cornelis@ing.nl;okwuagwucne@yahoo.com;xoxoyouknowitxoxo@hotmail.com;purdypervert@gmail.com;geneellis@ctc.net;son.doan@niteco.se;spatacus1988@yahoo.com;dohoangtung@gmail.com;teentitan1986@gmail.com;christianholmstrand@hotmail.com;contacto@lascarrascas.es;moody.20082010@gmail.com;buyerfromus@Hotmail.com;robert@uniqueue.se;tinawinston@valleytruckequipment.com;info@iprog.nl;borja.andres@gmail.com;ramonnaka@gmail.com;raja.s474@gmaIl.com;s_e_s_m@yahoo.com;lhbib95@gmail.com;normy273@hotmail.com;loulette999@live.fr;marcello.drewanz@wissens.com.br;kwcqgn@hcwcob.com;viswanath.etikala@gvkbio.com;sairam1806@gmail.com;uran3983@gmail.com;Pablo@prsoft.com;waytomyworld682@gmail.com;jcho95@hotmail.com;ola@sambergs.se;chettiars@dnbcb.com;benedetto.mcdonald@gmail.com;tracy.bowman@crye-leike.com;ville.laitila@mingraph.com;victor_augusto.gabriel@hotmail.com;siva.chimmani@gmail.com;xmr088@gmail.com;ivan.bolcina@apida.si;hitechmca@gmail.com;wesikc@knfycs.com;taylordubose@yahoo.com;peter.derwa@selfservicecompany.com;obantanahal@2xprime.com;skoyalkar@primetgi.com;joel@loop54.se;kingkero@gmail.com;harish217@yahoo.com;esmeralda.frank.castillo@gmail.com;nigel.alfred@gmail.com;abiiid@hotmail.com;kinijenika@gmail.com;wsf0001@auburn.edu;kqeulg@akqxmm.com;wxy2ab@sina.com;joakim.wennergren@neovici.se;lukasz.ciszak@hp.com;dyzhbb@bdtlup.com;jude.rae@gmail.com;johan.linnerhed@saabgroup.com;wallner.bg@gmail.com;lhussey@stonebond.com;cejchoj@gmail.com;hatsoykan@hotmail.com;costa@halicea.com;emilian.ionascu@think2solve.com;fahadcse07@gmail.com;adam@enigmaticflare.co.uk;val-imag@hotmail.com;geoffrey_forster@scientia.com;mr@magoo.fr;ph@realtime.co.th;rb@selligent.com;804193721@qq.com;happydvd@rediff.com;bluebalas@hotmail.com;flashkamilfia@ymail.com;info@4by4carhirekenya.com;gtorresrodriguez@gmail.com;gbanta@highspeed-solutions.net;hcullen01@gmail.com;pradeep4405@gmail.com;dear_angel0702@hotmail.com;lfav2012@gmail.com;djmzcin@yahoo.com;Don.whittaker@bestbuy.com;johnf442@aol.com;johne291@aol.com;alex.meyergleaves@gmail.com;dcdebock@gmail.com;ashesh1708@gmail.com;yuriaddict12345@aol.com;jingkun.lee@gmail.com;luismontbau@gmail.com;riccardo.tarli@txtgroup.com;starcounter@mailinator.com;abergheim@lodestonesolutions.com.au;peter@a2c.se;ssmart90@ymail.com;jeremychild@gmail.com;snezana.mitkova@yahoo.com;csavemark@hotmail.com;shurik@mail.zp.ua;sebastien-cote@hotmail.com;gre@feas.com;dbfuzz@hotmail.com;devendrakatuke@rediffmail.com;tomnuen@agiper.com;johna190@aol.com;johng233@aol.com;allan.udt@gmail.com;peter.schuermans@selfservicecompany.com;serdardirican@gmail.com;familyfun5@gmail.com;Yy@yy.com;patrick@videofy.me;Mhaerojo@yahoo.com;jazzyrainfire@gmail.com;johng23@aol.com;johna305@aol.com;s.limonta@kline.it;bjorn@edgrenalden.se;support@saba7host.com;Takanosaga999@gmail.com;lesbitcoil@gmail.com;spadsau@hotmail.com;ergun.ozyurt@joygame.com;marisolsantizo27@yahoo.com;g.saldi@kline.it;rtsucf@xztmbt.com;himanshu.solanki999@gmail.com;byusuk@gmx.de;umaribrahimawan@gmail.com;dave_cedric02@yahoo.com;info@artanddesign.com.gr;fleece@mail.ru;bjarne.christiansen@gmail.com;Venustark@gmail.com;danielchapman221@gmail.com;nikovv@gmail.com;darren_maglalang@yahoo.com;nuliknol@gmail.com;guitar_junkie_0912@yahoo.com;lovely_s12@yahoo.com;mxqueerco@gmail.com;jugqoh@qxvgyj.com;canbenegative@gmail.com;magnus@xsockets.net;samdei73@yahoo.com;sudhakar@fabergent.com;bb@millermalta.com;uzi_hiers@yahoo.co.uk;bhunter@innovasmarttechnologies.com;megumi_dwc@hotmail.com;aju_aju88@hotmail.com;dr0zaxx@gmail.com;bstearns@globalfinancialdata.com;www.sindhushankar@gmail.com;donyinaeric50@yahoo.com;SRHINGE1082@GMAIL.COM;jribeirosilva80@gmail.com;test3@test5.com;asa.holmstrom@starcounter.com;test3@test4.com;Nniizzaa03@gmail.com;mavullayya@gmail.com;everettsh1@gmail.com;sathyaco2000@gmail.com;mit.deepan@gmail.com;sjpzu@vodamail.co.za;myatkyawthu85@gmail.com;jani_26@abv.bg;habiburkhan98@yahoo.com;ddd@ddd.com;ccc@ccc.com;bbb@bbb.com;aaa@aaa.com;jckodel@gmail.com;gibbij@gmail.com;aaaa@aaa.com;alaaabdelatif1961@msn.com;omkingsanctuary9@gmail.com;sarazamora_24@hotmail.com;wayne.vos@gmail.com;Hanna.Nilsson@societyobjects.com;niklas@societyobjects.com;niklas.bjorkman@societyobjects.com;iliasushaibu@yahoo.com;kartikay2796@gmail.com;katikay2793@gmaol.com;jimmontross@gmail.com;baresaidoo@gmail.com;nopijambak@yahoo.com;per.jorlind@me.com;cbr@appearnetworks.com;yd0odd@yahoo.com;mallesh.santu@gmail.com;jitamitra.giet@gmail.com;natalia.szymacha@gmail.com;dcm-starcounter@capital-ideas.com;gabisreal@yahoo.com;archanad83@rocketmail.com;kumar.4gid@gmail.com;kirtisiwach@gmail.com;jsfnhngnnshaanika178@gmail.com;vishalbhormum@countryclub.com;yassinar_64@yahoo.com;excursion.hemant@gmail.com;jhal91@yahoo.com;eldani688@yahoo.co.id;chadani@sglg.asia.lk;don.cornforth@acgsystem.com;don.cornforth@gmail.com;rondro06@yahoo.fr;noli_inter@hotmail.com;SRIMARYANI21@ROCKETMAIL.COM;rahmat_kodja@yahoo.com;mohmed_f322@yahoo.com;www.yogaharjito@yahoo.co.id;innersakina@gmail.com;vinay@revinate.com;yanrkits@gmail.com;hakimsalim4@gmail.com;ad66@ukr.net;babyqurllovex3@aol.com;Dynakamalia@yahoo.com;d.nerada@shaw.ca;kmreddy_lichfl@rediffmail.com;subhash123456789smailbox@rediffmail.com;mun_bali@yahoo.com;silichungweekly@gmail.com;waldyhester@gmail.com;eman_super@yahoo.com;brainwaves01@hotmail.com;nurjahan.nesa@yahoo.com;anilpatel.ladol@gmail.com;mal873999@live.com;weix77@gmail.com;vivi.anne@windowslive.com;frel8817@gmail.com;frel8817@gmail.com;frel8817@gmail.com;frel8817@gmail.com;frel8817@gmail.com;frel8817@gmail.com;frel8817@gmail.com;26@example.com;prarom2011@hotmail.com;25@example.com;kostis.malkogiannis@starcounter.com;kostis@starcounter2.com;jadeebabes@hotmail.com;kamis2829.KHM@gmail.com;dbs101dbs@gmail.com;arielram@gmail.com;headline1118@hotmail.com;Xcunamix@gmail.com;kostasdre@yahoo.com;michaelayers@yahoo.com;v20jeanpiero20@hotmail.com;Masud787mbs@yahoo.com;vivimusi@gmail.com;umin.msmedia@gmail.com;Abanob.boney@yahoo.com;jan.berggren@neovici.se;regentblogs@gmail.com;lillyinfinfinito@gmail.com;hour_hour16872@yahoo.com;Chou_d500@yahoo.fr;ebruli93@gmail.com;Sreylenkhath@yahoo.com;tareqsaid19@hotmail.com;santi_espi85@hotmail.com;W002012@hotmail.com;Kadek1985@yahoo.co.id;sudhir.bhande@yahoo.com;johan.bergens@gmail.com;Franciscoalmeida1986@hotmail.com;wufong22@hotmail.com;suwat_wat1980@hotmail.com;perezmichael17@aol.com;Yinetjaquez032@hotmail.com;sayyadrizwanali@yahoo.co.in;communitydevelopment46@gmail.com;popolay629@gmail.com;manvendra.patel1@gmail.com;blackbutterfly.p95@gmail.com;jess_041581@yahoo.com;a_allam100@hotmail.com;mohamed.love88@yahoo.com;arshadbhutta@rockemail.com;martenc@gmail.com;soowaijian@hotmail.com;tinio2net@yahoo.com;asd@yhaoo.com;raaj_001@yahoo.co.uk;biltu.karmakar53@gmail.com;alankiza@hotmail.com;kizalan@hotmail.com;anup@ufpl.in;kamran_saeedi@yahoo.com;alasarzamana@yahoo.com;jasmen_rl@yahoo.com;mr.kyawthu999@gmail.com;beernardnkrumah89@yahoo.com;naveed_gaill@yahoo.com;laengdh@gmail.com;bjmarwa92@gmail.com;nandodarthvader@gmail.com;cferlyanne@yahoo.com;sandeshsalunkhe001@yahoo.co.in;bhandari.1990@yahoo.com;clickit.in@gmail.com;Ahmedsabri@yahoo.com;Shamsul.online@gmail.com;Narintip69@hotmail.com;feyisoa@yahoo.com;kachasen@gmial.com;doaa3x@yahoo.com;seza1978@gemail.com;valdyzone@rocketmail.com;Alar.lange@unit4.com;jhor_taurus@hotmail.com;ahmadtarek32@hotmail.com;Developer@wirescience.com;fbenour@yahoo.fr;sissy_boboy@hotmail.com;wazeem.rock@yahoo.com;Naglaa.dewedar@yahoo.com;hind_h75@hotmail.com;Janeslowson@gmail.com;froy.2ramirez@gmail.com;liyemasecurity@gmail.com;tchella9@hotmail.fr;Hooksng@yahoo.com;21@example.com;usman696@yahoo.com;modi-mk-98@live.com;uddin.rayhan52@yahoo.com;mina_nabil9@yahoo.com;ulf.collin@hm.com;alcaesarabogamal89@hotmail.com;saade00@hotmail.com;alviansyahciicutt@gmai.com;binod.kumar674@gmail.com;zahretalyasamen-nadoush-love@hotmail.com;guillem1944@hotmail.com;narek.minassian@yandex.ru;ramesh.devycs@gmail.com;dennyyow_39@live.com;msalim.tawhid3@gmail.com;ahmad.r2011@yahoo.com;khan92142@gmail.com;prveen.dashing2007@gmail.com;shameersheriff100@gmail.com;girish@gmail.com;girish9188@gmail.com;mr.bombaztic_27@yahoo.com;woollyeggranch@aol.com;honken_kc99@hotmail.com;blueblabla@gmx.ch;muneebshahzad1990@yahoo.com;urcdmo@lmyhbx.com;16@example.com;15@example.com;13@example.com;12@example.com;333@example.com;33@example.com;this_is_a_test_email2@example.com;this_is_a_test_email@example.com;10@example.com;9@example.com;8@example.com;7@example.com;6@example.com;5@example.com;clyztfrd@yfqmnray.com;4@example.com;3@example.com;2@example.com;martinnissfolk@gmail.com;1@example.com;martinnissfolk@hotmail.com;francis.ellison@skype.com;jagflashback@hotmail.com;m.emostar@yahoo.com;drakod@live.com;harshal@snehapadma.com;ahnold@rocketmail.com;devicecontext@hotmail.com;kirbakalim@hotmail.com;lesovandrey@gmail.com;admin@oskol.us;stefan.ekholm@nj.se;ks@myweightdoctor.com;therealgrimbly@gmail.com;jr@aol.com;pjones0619@rogers.com;corneliuswilkins@hotmail.com;imran.haider@kabotintl.com;seun104@gmail.com;satish860@gmail.com;Andy.james@solidsoft.com;widowblade@yahoo.com;alashcraft@gmail.com;carlos26vz@hotmail.com;jeppe@cramon.dk;qazzysiw@uhjrnito.com;fabio@engenhariadesoftware.inf.br;david.bjorklund@gmail.com;ekacharoe@yahoo.co.uk;antonphamvanbinh@yahoo.com.vn;buzyjiang@gmail.com;felicia.holmstrom@gmail.com;super@lynhurtig.dk;err@email.com;o0h-15@hotmail.com;alexmo@gmail.com;meggiesp8@yahoo.com;jjb@12move.dk;kyle@massivehealth.com;test@test.com;steve.flitcroft@gmail.com;muarysol@hotmail.com;longnhi2908@gmail.com;hoang.tran@niteco.se;abi_ihsan@ymail.com;jdmcbride@iee.org;ma@amasis.de;roilaanish@gmail.com;silenceinthescream@hotmail.ca;alex_os@rogers.com;bbixler@deloitte.com;kamalsoltani@gmail.com;chronik@zworg.com;jad.gaspar@gmail.com;vikramsawariya@yahoo.co.in;onesisnunez@yahoo.com;ChaiFreakingTea@gmail.com;liudan_seven@sina.com;Eric.berntson@Snallerods.se;miloszkukla@o2.pl;admin@katehewittonline.co.uk;capgeminimobiletest@gmail.com;abcd1234@hotmail.com;leovpachon@gmail.com;tunamenu@gmail.com;choiceblack@gmail.com;llausas2001@gmail.com;puisto.kemisti@hotmail.com;anyspam2@gmail.com;slyman1011@hotmail.com;eglise150@aol.com;robert@devrex.se;tony.fox@berkeleycannon.com;dk@pensio.dk;gyroid@hotmail.ca;ricardo.wickel@bewotec.de;tfischer@tangible.de;mishkatahmed420@gmail.com;nayanahmed420@yahoo.com;abdoulienjie21@gmail.com;willweiss@me.com;wallick.tom@gmail.com;fredrik@reiback.se;peter16byrne@hotmail.com;asa.liljegren@thomascook.se;joakim.christopherson@thomascook.se;andlju@gmail.com;ola.gunnars@bahnhof.se;ben.hallam@live.com.au;mcnkbr@gmail.com;marcin@nextgen.pl;niklas.bjorkman@starcounter.com;joachimwester@me.com;christian.holmstrand@starcounter.com;per@starcounter.com;Ruslan.Fomkin@starcounter.com;anders.wahlgren@societyobjects.com;jungkun.lee@gmail.com";
            //string[] emails = str.Split(';');

            //foreach (string email in emails) {

            //    string downloadKey = GetUniqueDownloadKey();
            //    if (downloadKey == null) {
            //        LogWriter.WriteLine("ERROR: Could not generate unique id, email import aborted");
            //        return false;
            //    }
            //    Db.Transaction(() => {

            //        Somebody s = new Somebody();
            //        s.DownloadKey = downloadKey;
            //        s.Email = email;
            //    });
            //}
            return true;
        }

        /// <summary>
        /// Generate unique download key
        /// </summary>
        /// <returns>key otherwise null</returns>
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


        /// <summary>
        /// Check if a directory is empty
        /// </summary>
        /// <param name="path"></param>
        /// <returns>True if directory is empty otherwise false</returns>
        public static bool IsDirectoryEmpty(string path) {
            //    return !Directory.EnumerateFileSystemEntries(path).Any();
            IEnumerable<string> items = Directory.EnumerateFileSystemEntries(path);
            using (IEnumerator<string> en = items.GetEnumerator()) {
                return !en.MoveNext();
            }
        }
    }
}
